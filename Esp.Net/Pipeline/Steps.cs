using System;
using Esp.Net.Model;
using Esp.Net.Reactive;
using Esp.Net.RxBridge;

namespace Esp.Net.Pipeline
{
    public enum StepType
    {
        Async,
        Sync
    }

    public abstract class Step<TModel> : DisposableBase
    {
        public abstract StepType Type { get; }

        public abstract IObservable<TModel> ExecuteAcync(TModel model);
        public abstract void Execute(TModel model);
    }

    public class StepResult
    {
        public static StepResult Cancel()
        {
            return new StepResult(false);
        }

        public static StepResult Continue()
        {
            return new StepResult(true);
        }

        public StepResult(bool executeStep)
        {
            ExecuteStep = executeStep;
        }

        public bool ExecuteStep { get; protected set; }
    }

    public class StepResult<TResult>
    {
        public static StepResult<TResult> Cancel()
        {
            return new StepResult<TResult>(false);
        }

        public static StepResult<TResult> Continue(IObservable<TResult> resultStream)
        {
            return new StepResult<TResult>(resultStream);
        }

        private StepResult(bool executeStep)
        {
            ExecuteStep = executeStep;
        }

        private StepResult(IObservable<TResult> resultStream)
        {
            ExecuteStep = true;
            ResultStream = resultStream;
        }

        public bool ExecuteStep { get; private set; }

        public IObservable<TResult> ResultStream { get; private set; }
    }

    public class AsyncStep<TModel, TAsyncResults> : Step<TModel>
    {
        private readonly IRouter<TModel> _router;
        private readonly Func<TModel, StepResult<TAsyncResults>> _onBegin;
        private readonly Action<TModel, TAsyncResults> _onAsyncResults;

        public AsyncStep(IRouter<TModel> router, Func<TModel, StepResult<TAsyncResults>> onBegin, Action<TModel, TAsyncResults> onAsyncResults)
        {
            _router = router;
            _onBegin = onBegin;
            _onAsyncResults = onAsyncResults;
        }

        public override StepType Type
        {
            get { return StepType.Async; }
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

    public class SyncStep<TModel> : Step<TModel>
    {
        private readonly Func<TModel, StepResult> _action;

        public SyncStep(Func<TModel, StepResult> action)
        {
            _action = action;
        }

        public override StepType Type
        {
            get { return StepType.Sync; }
        }

        public override IObservable<TModel> ExecuteAcync(TModel model)
        {
            throw new InvalidOperationException();
        }

        public override void Execute(TModel model)
        {
            _action(model);
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
}