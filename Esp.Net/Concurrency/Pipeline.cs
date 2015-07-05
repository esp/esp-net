#if ESP_EXPERIMENTAL

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Esp.Net.Model;
using System.Reactive.Linq;

namespace Esp.Net.Concurrency
{
    public interface IPipelineInstanceContext<out TEvent>
    {
        TEvent Event{ get; }
    }

    public static class PipelineRouterExt
    {
        public static PipelineBuilder<TModel> ConfigurePipeline<TModel>(this IRouter<TModel> router)
        {
            return new PipelineBuilder<TModel>(router);
        }
    }

    public interface IPipeline<in TModel>
    {
        IPipelineInstance<TModel> CreateInstance();
    }

    public interface IPipelineInstance<in TModel> : IDisposable
    {
        void Run(TModel currentModel, Action<Exception> onError = null);
    }

//    public interface IWorkItemBuilder<out TModel>
//    {
//        IWorkItemStepBuilder<TModel> OnEvent<TEvent>(
//            Action<TModel, TEvent, IEventContext> onEvent,
//            ObservationStage stage = ObservationStage.Normal
//        );
//    }
//
//    public interface IWorkItemStepBuilder<out TModel>
//    {
//        IWorkItemStepBuilder<TModel> Do(Action<TModel> onEvent);
//        IWorkItemStepBuilder<TModel> SubscribeTo<TResult>(
//            Func<TModel, IObservable<TResult>> observableFactory,
//            Action<TModel, TResult> onResultsReceived
//        );
//        IDisposable Run();
//    }

    public class PipelineBuilder<TModel> // : IWorkItemBuilder<TModel>, IWorkItemStepBuilder<TModel>
    {
        private readonly IRouter<TModel> _router;
        private readonly List<Step<TModel>> _steps = new List<Step<TModel>>(); 

        public PipelineBuilder(IRouter<TModel> router)
        {
            _router = router;
        }

//        public IWorkItemStepBuilder<TModel> OnEvent<TEvent>(
//            Action<TModel, TEvent, IEventContext> onEvent,
//            ObservationStage stage = ObservationStage.Normal
//        )
//        {
////            var step = new ObservableStep<TModel, TResult>(_router, observableFactory, onResultsReceived);
////            _steps.Add(step);
//            return this;
//        }

//        public IWorkItemStepBuilder<TModel> Do(Action<TModel> onEvent)
//        {
//            //            var step = new ObservableStep<TModel, TResult>(_router, observableFactory, onResultsReceived);
//            //            _steps.Add(step);
//            return this;
//        }

//        public IDisposable Run()
//        {
//            return null;
//        }

        public PipelineBuilder<TModel> SubscribeTo<TResult>(
            Func<TModel, IObservable<TResult>> observableFactory,
            Action<TModel, TResult> onResultsReceived
        )
        {
            var step = new ObservableStep<TModel, TResult>(_router, observableFactory, onResultsReceived);
            _steps.Add(step);
            return this;
        }

        public IPipeline<TModel> Create()
        {
            return new Pipeline<TModel>(_steps);
        }
    }

    public class Pipeline<TModel> : DisposableBase, IPipeline<TModel>
    {
        private readonly List<Step<TModel>> _steps;

        public Pipeline(List<Step<TModel>> steps)
        {
            _steps = steps;
        }

        public IPipelineInstance<TModel> CreateInstance()
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
        private class PipelineInstance : DisposableBase, IPipelineInstance<TModel>
        {
            private readonly Step<TModel> _firstStep;
            private Action< Exception> _onError;
            private readonly Queue<Action<TModel>> _queue = new Queue<Action<TModel>>();
            private bool _purging;

            public PipelineInstance(Step<TModel> firstStep)
            {
                _firstStep = firstStep;
            }

            public void Run(TModel currentModel, Action<Exception> onError = null)
            {
                _onError = onError;
                _queue.Enqueue(CreateStep(_firstStep));
                PurgeQueue(currentModel);
            }

            private Action<TModel> CreateStep(Step<TModel> step)
            {
                return (currentModel) =>
                {
                    if (step.Type == StepType.Async)
                    {
                        IDisposable stepDisposable = EspDisposable.Empty;
                        // note that the step may yield multiple times and we just stay subscribed until it 
                        // errors or completes. This means we may run a step once, then run subsequent steps 
                        // multiple times. 
                        stepDisposable = step.GetExecuteStream(currentModel).Subscribe(latestModel =>
                        {
                            if (step.Next != null)
                            {
                                _queue.Enqueue(CreateStep(step.Next));
                                PurgeQueue(latestModel);
                            }
                        },
                        ex =>
                        {
                            if (_onError == null)
                            {
                                throw ex;
                            }
                            _onError(ex);
                        },
                        () =>
                        {
                            // need to dispose of child steps        
                        });
                        AddDisposable(stepDisposable);
                    }
                    else
                    {
                        step.Execute(currentModel);
                        if (step.Next != null)
                        {
                            _queue.Enqueue(CreateStep(step.Next));
                            PurgeQueue(currentModel);
                        }
                    }
                };
            }

            private void PurgeQueue(TModel currentModel)
            {
                Debug.Assert(!_purging);
                _purging = true;
                try
                {
                    var hasItems = _queue.Count > 0;
                    while (hasItems)
                    {
                        var action = _queue.Dequeue();
                        action(currentModel);
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
}
#endif