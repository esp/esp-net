#if ESP_EXPERIMENTAL

using System;
using System.Collections.Generic;
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
            return new PipelineInstance(_steps);
        }

        // it's entirely possible that a pipeline instance is never disposed, it may just run it's course. 
        // however it if it's disposed before this point father step won't be run.
        private class PipelineInstance : DisposableBase, IPipelinInstance<TModel>
        {
            private readonly List<Step<TModel>> _steps;
            private Action< Exception> _onError;

            public PipelineInstance(List<Step<TModel>> steps)
            {
                _steps = steps;
            }

            public void Run(TModel currentModel, Action<Exception> onError = null)
            {
                _onError = onError;
                RunStep(0, currentModel);
            }

            private void RunStep(int stepIndex, TModel currentModel)
            {
                var hasStepsLeft = _steps.Count > stepIndex;
                if (hasStepsLeft)
                {
                    var step1 = _steps[stepIndex];
                    if (step1.Type == StepType.Async)
                    {
                        IDisposable stepDisposable = EspDisposable.Empty;
                        // note that the step1 may yield multiple times and we just stay subscribed until it 
                        // errors or completes. This means we may run a step once, then run subsequent steps 
                        // multiple times. 
                        stepDisposable = step1.ExecuteAcync(currentModel).Subscribe(latestModel =>
                        {
                            RunStep(++stepIndex, latestModel);
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
                        step1.Execute(currentModel);
                        RunStep(++stepIndex, currentModel);
                    }
                }
            }
        }
    }
}
#endif