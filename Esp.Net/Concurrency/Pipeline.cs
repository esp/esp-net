#if ESP_EXPERIMENTAL

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Esp.Net.Model;
using System.Reactive.Linq;

namespace Esp.Net.Concurrency
{
    public interface IPipelineInstanceContext
    {
        bool IsCanceled { get; }
        void Cancel();
    }

    public static class PipelineRouterExt
    {
        public static PipelineBuilder<TModel, DefatultPipelineInstanceContext, TInitialEvent> ConfigurePipeline<TModel, TInitialEvent>(
            this IRouter<TModel> router
        )
        {
            return new PipelineBuilder<TModel, DefatultPipelineInstanceContext, TInitialEvent>(
                router, 
                (m, e, c) => new DefatultPipelineInstanceContext()
            );
        }

        public static PipelineBuilder<TModel, TPipelineContext, TInitialEvent> ConfigurePipeline<TModel, TPipelineContext, TInitialEvent>(
            this IRouter<TModel> router,
            Func<TModel, TInitialEvent, IEventContext, TPipelineContext> contextFactory
        ) 
            where TPipelineContext : IPipelineInstanceContext
        {
            return new PipelineBuilder<TModel, TPipelineContext, TInitialEvent>(router, contextFactory);
        }
    }

    public interface IPipeline<in TModel, TPipelineContext>
        where TPipelineContext : IPipelineInstanceContext
    {
        IPipelineInstance<TModel, TPipelineContext> CreateInstance();
    }

    public interface IPipelineInstance<in TModel, TPipelineContext> : IDisposable
        where TPipelineContext : IPipelineInstanceContext
    {
        void Run(TModel currentModel, TPipelineContext context, Action<TPipelineContext, Exception> onError = null);
    }

    public class PipelineBuilder<TModel, TPipelineContext, TInitialEvent> 
        where TPipelineContext : IPipelineInstanceContext
    {
        private readonly IRouter<TModel> _router;
        private readonly Func<TModel, TInitialEvent, IEventContext, TPipelineContext> _contextFactory;
        private readonly List<Step<TModel, TPipelineContext>> _steps = new List<Step<TModel, TPipelineContext>>();

        public PipelineBuilder(
            IRouter<TModel> router,
            Func<TModel, TInitialEvent, IEventContext, TPipelineContext> contextFactory)
        {
            _router = router;
            _contextFactory = contextFactory;
        }

        public PipelineBuilder<TModel, TPipelineContext, TInitialEvent> SelectMany<TResult>(
            Func<TModel, TPipelineContext, IObservable<TResult>> observableFactory,
            Action<TModel, TResult> onResultsReceived
        )
        {
            var step = new ObservableStep<TModel, TPipelineContext, TResult>(_router, observableFactory, onResultsReceived);
            _steps.Add(step);
            return this;
        }

        public PipelineBuilder<TModel, TPipelineContext, TInitialEvent> Do(
            Action<TModel, TPipelineContext> action
        )
        {
            var step = new SyncStep<TModel, TPipelineContext>(action);
            _steps.Add(step);
            return this;
        }
        
        public IPipeline<TModel, TPipelineContext> Create()
        {
            return new Pipeline<TModel, TPipelineContext>(_steps);
        }

        public IDisposable Run(Action<TPipelineContext, Exception> onError)
        {
            var pipeline = Create();
            return _router.GetEventObservable<TInitialEvent>().Observe((m, e, c) =>
            {
                IPipelineInstance<TModel, TPipelineContext> pipelineInstance = pipeline.CreateInstance();
                TPipelineContext pipelineInstanceContext = _contextFactory(m, e, c);
                pipelineInstance.Run(m, pipelineInstanceContext);
            });
        }
    }

    public class Pipeline<TModel, TPipelineContext> : DisposableBase, IPipeline<TModel, TPipelineContext>
        where TPipelineContext : IPipelineInstanceContext
    {
        private readonly List<Step<TModel, TPipelineContext>> _steps;

        public Pipeline(List<Step<TModel, TPipelineContext>> steps)
        {
            _steps = steps;
        }

        public IPipelineInstance<TModel, TPipelineContext> CreateInstance()
        {
            var firstStep = _steps[0];
            for (int i = 1; i < _steps.Count; i++)
            {
                firstStep.Next = _steps[i];
            }
            return new PipelineInstance(firstStep);
        }

        // it's entirely possible that a Pipeline instance is never disposed, it may just run it's course. 
        // however it if it's disposed before this point father step won't be run.
        private class PipelineInstance : DisposableBase, IPipelineInstance<TModel, TPipelineContext>
        {
            private readonly Step<TModel, TPipelineContext> _firstStep;
            private Action<TPipelineContext, Exception> _onError;
            private readonly Queue<Action<TModel, TPipelineContext>> _queue = new Queue<Action<TModel, TPipelineContext>>();
            private bool _purging;

            public PipelineInstance(Step<TModel, TPipelineContext> firstStep)
            {
                _firstStep = firstStep;
            }

            public void Run(TModel currentModel, TPipelineContext context, Action<TPipelineContext, Exception> onError = null)
            {
                _onError = onError;
                _queue.Enqueue(CreateStep(_firstStep));
                PurgeQueue(currentModel, context);
            }

            private Action<TModel, TPipelineContext> CreateStep(Step<TModel, TPipelineContext> step)
            {
                return (currentModel, context) =>
                {
                    if (step.Type == StepType.Async)
                    {
                        IDisposable stepDisposable = EspDisposable.Empty;
                        // note that the step may yield multiple times and we just stay subscribed until it 
                        // errors or completes. This means we may run a step once, then run subsequent steps 
                        // multiple times. 
                        stepDisposable = step.GetExecuteStream(currentModel, context).Subscribe(latestModel =>
                        {
                            if (step.Next != null)
                            {
                                _queue.Enqueue(CreateStep(step.Next));
                                PurgeQueue(latestModel, context);
                            }
                        },
                        ex =>
                        {
                            if (_onError == null)
                            {
                                throw ex;
                            }
                            _onError(context, ex);
                        },
                        () =>
                        {
                            // need to dispose of child steps        
                        });
                        AddDisposable(stepDisposable);
                    }
                    else
                    {
                        step.Execute(currentModel, context);
                        if (step.Next != null)
                        {
                            _queue.Enqueue(CreateStep(step.Next));
                            PurgeQueue(currentModel, context);
                        }
                    }
                };
            }

            private void PurgeQueue(TModel currentModel, TPipelineContext context)
            {
                Debug.Assert(!_purging);
                _purging = true;
                try
                {
                    var hasItems = _queue.Count > 0;
                    while (hasItems)
                    {
                        var action = _queue.Dequeue();
                        action(currentModel, context);
                        hasItems = _queue.Count > 0;
                    }
                }
                finally
                {
                    _purging = false;
                }
            }
        }
    }

    public class DefatultPipelineInstanceContext : IPipelineInstanceContext
    {
        private bool _isCanceled;

        public bool IsCanceled
        {
            get { return _isCanceled; }
        }

        public void Cancel()
        {
            if (_isCanceled) throw new InvalidOperationException("Already canceled");
            _isCanceled = true;
        }
    }
}
#endif