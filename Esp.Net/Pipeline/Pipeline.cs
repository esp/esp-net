using System;
using System.Collections.Generic;
using Esp.Net.Model;
using Esp.Net.RxBridge;

namespace Esp.Net.Pipeline
{
    public interface IPipeline<in TModel>
    {
        IPipelinInstance<TModel> CreateInstance();
    }

    public interface IPipelinInstance<in TModel> : IDisposable
    {
        void Run(TModel currentModel);
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
            var step = new AsyncStep<TModel, TResult>(_router, onBegin, onAsyncResults);
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

        private class PipelineInstance : DisposableBase, IPipelinInstance<TModel>
        {
            private readonly List<Step<TModel>> _steps;

            public PipelineInstance(List<Step<TModel>> steps)
            {
                _steps = steps;
            }

            public void Run(TModel initialModel)
            {
                RunStep(0, initialModel);
            }

            private void RunStep(int stepIndex, TModel currentModel)
            {
                if (_steps.Count > stepIndex)
                {
                    var step1 = _steps[stepIndex];
                    if (step1.Type == StepType.Async)
                    {
                        IDisposable stepDisposable = EspDisposable.Empty;
                        stepDisposable = step1.ExecuteAcync(currentModel).Subscribe(latestModel =>
                        {
                            stepDisposable.Dispose();
                            RunStep(++stepIndex, latestModel);
                        });
                        AddDisposable(stepDisposable);
                    }
                    else
                    {
                        step1.Execute(currentModel);
                        RunStep(++stepIndex, currentModel);
                    }
                }
                else
                {
                    // dispose?
                }
            }
        }
    }
}