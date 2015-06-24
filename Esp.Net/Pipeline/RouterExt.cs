using System;
using System.Collections.Generic;
using Esp.Net.Model;

namespace Esp.Net.Pipeline
{
    public static class RouterExt
    {
        public static PipelineBuilder<TModel> CreatePipeline<TModel>(this IRouter<TModel> router)
        {
            return new PipelineBuilder<TModel>(router);
        }
    }

    public class PipelineBuilder<TModel>
    {
        private readonly IRouter<TModel> _router;
        private readonly List<PipelineStep> _steps = new List<PipelineStep>(); 

        public PipelineBuilder(IRouter<TModel> router)
        {
            _router = router;
        }

        public PipelineBuilder<TModel> On<TInitialEvent>(Func<TModel, IEventContext<TModel, TInitialEvent>, PipelineStepResult> onReceived)
        {
            var step = new InitialPipelineStep<TModel, TInitialEvent>(_router, onReceived);
            _steps.Add(step);
            return this;
        }

        public PipelineBuilder<TModel> AddStep<TResult>(
            Func<TModel, PipelineStepResult<TResult>> onBegin,
            Action<TModel, TResult> onAsyncResults
        )
        {
            var step = new AsyncPipelineStep<TModel, TResult>(_router, onBegin, onAsyncResults);
            _steps.Add(step);
            return this;
        }

        public IPipeline Build()
        {
            return new Pipeline<TModel>(_router, _steps);
        }
    }

    public abstract class PipelineStep : DisposableBase
    {
        public abstract PipelineStepType Type { get; }
    }

    public enum PipelineStepType
    {
        Async,
        Sync
    }

    public class InitialPipelineStep<TModel, TInitialEvent> : PipelineStep
    {
        private readonly IRouter<TModel> _router;

        public InitialPipelineStep(IRouter<TModel> router, Func<TModel, IEventContext<TModel, TInitialEvent>, PipelineStepResult> onReceived)
        {
            _router = router;
            throw new NotImplementedException();
        }

        public override PipelineStepType Type
        {
            get { return PipelineStepType.Async; }
        }

        public void Run()
        {
            AddDisposable(_router.GetEventObservable<TInitialEvent>().Observe(context =>
            {
                
            }));
        }
    }

    public class AsyncPipelineStep<TModel, TAsyncResults> : PipelineStep
    {
        private readonly IRouter<TModel> _router;
        private readonly Func<TModel, PipelineStepResult<TAsyncResults>> _onBegin;
        private readonly Action<TModel, TAsyncResults> _onAsyncResults;

        public AsyncPipelineStep(IRouter<TModel> router, Func<TModel, PipelineStepResult<TAsyncResults>> onBegin, Action<TModel, TAsyncResults> onAsyncResults)
        {
            _router = router;
            _onBegin = onBegin;
            _onAsyncResults = onAsyncResults;
        }

        public override PipelineStepType Type
        {
            get { return PipelineStepType.Async; }
        }

        public void Start(TModel model)
        {
            var stepResults = _onBegin(model);
            if (stepResults.ExecuteStep)
            {

            }
        }
    }

    public class Pipeline<TModel/* TState*/> : IPipeline
    {
        private readonly IRouter<TModel> _router;
        private readonly List<PipelineStep> _steps;

        public Pipeline(IRouter<TModel> router, List<PipelineStep> steps)
        {
            _router = router;
            _steps = steps;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void CreateInstance()
        {
            var step1 = _steps[0];

            

            throw new NotImplementedException();
        }


        private void RunStep(PipelineStep step)
        {
            
        }

        public void AddStep<TAsyncResults>(AsyncPipelineStep<TModel, TAsyncResults> step)
        {
            
        }
    }

    public interface IPipeline : IDisposable
    {
        void CreateInstance(/*TState state*/);
    }

    public class PipelineStepResult
    {
        public static PipelineStepResult DontRun()
        {
            return new PipelineStepResult(false);
        }

        public static PipelineStepResult Run()
        {
            return new PipelineStepResult(true);
        }

        public PipelineStepResult(bool executeStep)
        {
            ExecuteStep = executeStep;
        }

        public bool ExecuteStep { get; protected set; }
    }

    public class PipelineStepResult<TResult> 
    {
        public static PipelineStepResult<TResult> DontRun()
        {
            return new PipelineStepResult<TResult>(false);
        }

        public static PipelineStepResult<TResult> Run(IObservable<TResult> resultStream)
        {
            return new PipelineStepResult<TResult>(resultStream);
        }

        private PipelineStepResult(bool executeStep)
        {
            ExecuteStep = executeStep;
        }

        public PipelineStepResult(IObservable<TResult> resultStream)
        {
            ExecuteStep = true;
            ResultStream = resultStream;
        }

        public bool ExecuteStep { get; private set; }

        public IObservable<TResult> ResultStream { get; private set; }
    }
}