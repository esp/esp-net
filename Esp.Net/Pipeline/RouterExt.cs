using System;
using System.Collections.Generic;
using Esp.Net.Model;
using Esp.Net.Pipeline.ReactiveBridge;
using Esp.Net.Reactive;

namespace Esp.Net.Pipeline
{
    public static class RouterExt
    {
        public static PipelineBuilder<TModel> ConfigurePipeline<TModel>(this IRouter<TModel> router)
        {
            return new PipelineBuilder<TModel>(router);
        }
    }

    public class PipelineBuilder<TModel>
    {
        private readonly IRouter<TModel> _router;
        private readonly List<PipelineStep<TModel>> _steps = new List<PipelineStep<TModel>>(); 

        public PipelineBuilder(IRouter<TModel> router)
        {
            _router = router;
        }

        public PipelineBuilder<TModel> On<TInitialEvent>(Func<IEventContext<TModel, TInitialEvent>, PipelineStepResult> onBegin, ObservationStage stage = ObservationStage.Normal)
        {
            var step = new InitialPipelineStep<TModel, TInitialEvent>(_router, onBegin, stage);
            _steps.Add(step);
            return this;
        }

        public PipelineBuilder<TModel> Then<TResult>(
            Func<TModel, PipelineStepResult<TResult>> onBegin,
            Action<TModel, TResult> onAsyncResults
        )
        {
            var step = new AsyncPipelineStep<TModel, TResult>(_router, onBegin, onAsyncResults);
            _steps.Add(step);
            return this;
        }

        public IPipeline Create()
        {
            return new Pipeline<TModel>(_router, _steps);
        }
    }

    public abstract class PipelineStep<TModel> : DisposableBase
    {
        public abstract PipelineStepType Type { get; }

        public abstract IObservable<TModel> ExecuteAcync(TModel model);
        public abstract void Execute(TModel model);
    }

    public enum PipelineStepType
    {
        Async,
        Sync
    }

    public class InitialPipelineStep<TModel, TInitialEvent> : PipelineStep<TModel>
    {
        private readonly IRouter<TModel> _router;
        private readonly Func<IEventContext<TModel, TInitialEvent>, PipelineStepResult> _onBegin;
        private readonly ObservationStage _stage;

        public InitialPipelineStep(IRouter<TModel> router, Func<IEventContext<TModel, TInitialEvent>, PipelineStepResult> onBegin, ObservationStage stage)
        {
            _router = router;
            _onBegin = onBegin;
            _stage = stage;
        }

        public override PipelineStepType Type
        {
            get { return PipelineStepType.Async; }
        }

        public override IObservable<TModel> ExecuteAcync(TModel model)
        {
            return EspObservable.Create<TModel>(o =>
            {
                return _router.GetEventObservable<TInitialEvent>(_stage).Observe(context =>
                {
                    var stepResults = _onBegin(context);
                    if (stepResults.ExecuteStep)
                    {
                        o.OnNext(context.Model);
                    }
                    else
                    {
                        o.OnCompleted();
                    }
                });
            });
        }

        public override void Execute(TModel model)
        {
            throw new InvalidOperationException();
        }
    }

    public class AsyncPipelineStep<TModel, TAsyncResults> : PipelineStep<TModel>
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

        public override IObservable<TModel> ExecuteAcync(TModel model)
        {
            return EspObservable.Create<TModel>(o =>
            {
                var disposables = new DisposableCollection();
                var stepResults = _onBegin(model);
                if (stepResults.ExecuteStep)
                {
                    var id = Guid.NewGuid();
                    disposables.Add(
                        _router
                            .GetEventObservable<AsyncResultsRecenvedEvent<TAsyncResults>>()
                            .Where(context => context.Event.Id == id)
                            .Observe(context =>
                            {
                                _onAsyncResults(context.Model, context.Event.Result);
                                o.OnNext(model);
                                o.OnCompleted(); // ?? maybe
                            }
                        )
                    );
                    disposables.Add(stepResults.ResultStream.Subscribe(result => {
                        _router.PublishEvent(new AsyncResultsRecenvedEvent<TAsyncResults>(result, id));
                    }));
                }
                else
                {
                    o.OnCompleted();
                }
                return disposables;
            });
        }

        public override void Execute(TModel model)
        {
            throw new InvalidOperationException();
        }
    }

    public class AsyncResultsRecenvedEvent<TResult>
    {
        public AsyncResultsRecenvedEvent(TResult result, Guid id)
        {
            Result = result;
            Id = id;
        }

        public TResult Result { get; private set; }

        public Guid Id { get; private set; }
    }

    public class Pipeline<TModel/* TState*/> : DisposableBase, IPipeline
    {
        private readonly IRouter<TModel> _router;
        private readonly List<PipelineStep<TModel>> _steps;

        public Pipeline(IRouter<TModel> router, List<PipelineStep<TModel>> steps)
        {
            _router = router;
            _steps = steps;
        }

        public void CreateInstance()
        {
            var step1 = _steps[0];
            

            throw new NotImplementedException();
        }
//
//
//        private void RunStep(PipelineStep step)
//        {
//            
//        }
//
//        public void Then<TAsyncResults>(AsyncPipelineStep<TModel, TAsyncResults> step)
//        {
//            
//        }

        public void RunStep(int stepIndex, TModel currentModel)
        {
            if (_steps.Count < stepIndex)
            {
                var step1 = _steps[stepIndex];
                if (step1.Type == PipelineStepType.Async)
                {
                    var d = step1.ExecuteAcync(currentModel).Subscribe(model =>
                    {
                        RunStep(++stepIndex, model);
                    });

                    //                AddDisposable(_router.GetEventObservable<TInitialEvent>().Observe(context =>
                    //                {
                    //                    var results = _onReceived(context);
                    //                    if (results.ExecuteStep)
                    //                    {
                    //
                    //                    }
                    //                }));
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