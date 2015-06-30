#if ESP_EXPERIMENTAL

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Esp.Net.Model;
using System.Reactive.Linq;

namespace Esp.Net.Concurrency
{
    public static class PipelineRouterExt
    {
        public static PipelineBuilder<TModel> ConfigurePipeline<TModel>(this IRouter<TModel> router)
        {
            return new PipelineBuilder<TModel>(router);
        }
    }

    public interface IPipeline<in TModel>
    {
        IPipelinInstance<TModel> CreateInstance();
    }

    public interface IPipelinInstance<in TModel> : IDisposable
    {
        void Run(TModel currentModel, Action<Exception> onError = null);
    }

    public class PipelineBuilder<TModel>
    {
        private readonly IRouter<TModel> _router;
        private readonly List<Step<TModel>> _steps = new List<Step<TModel>>(); 

        public PipelineBuilder(IRouter<TModel> router)
        {
            _router = router;
        }

        public PipelineBuilder<TModel> AddStep<TResult>(
            Func<TModel, StepResult<TResult>> onBegin,
            Action<TModel, TResult> onAsyncResults
            )
        {
            var step = new ObservableStep<TModel, TResult>(_router, onBegin, onAsyncResults);
            _steps.Add(step);
            return this;
        }

        public PipelineBuilder<TModel> AddStep(
            Func<TModel, StepResult> action
            )
        {
            var step = new SyncStep<TModel>(action);
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

        public IPipelinInstance<TModel> CreateInstance()
        {
            var firstStep = _steps[0];
            for (int i = 1; i < _steps.Count; i++)
            {
                firstStep.Next = _steps[i];
            }
            return new PipelineInstance(firstStep);
        }

        // it's entirely possible that a pipeline instance is never disposed, it may just run it's course. 
        // however it if it's disposed before this point father step won't be run.
        private class PipelineInstance : DisposableBase, IPipelinInstance<TModel>
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
                        stepDisposable = step.ExecuteAcync(currentModel).Subscribe(latestModel =>
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