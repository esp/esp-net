#if ESP_EXPERIMENTAL
using System;
using System.Reactive.Linq;
using Esp.Net.Model;
using Esp.Net.Reactive;

namespace Esp.Net.Concurrency
{
    public enum StepType
    {
        Async,
        Sync
    }

    public abstract class Step<TModel> : DisposableBase
    {
        public abstract StepType Type { get; }

        public abstract IObservable<TModel> ExecuteStream(TModel model);
        
        public abstract void Execute(TModel model);

        public Step<TModel> Next { get; set; }
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

        public static StepResult<TResult> SubscribeTo(IObservable<TResult> resultStream)
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

        internal bool ExecuteStep { get; private set; }

        public IObservable<TResult> ResultStream { get; private set; }
    }

    public class ObservableStep<TModel, TResults> : Step<TModel>
    {
        private readonly IRouter<TModel> _router;
        private readonly Func<TModel, StepResult<TResults>> _onBegin;
        private readonly Action<TModel, TResults> _onAsyncResults;

        public ObservableStep(IRouter<TModel> router, Func<TModel, StepResult<TResults>> onBegin, Action<TModel, TResults> onAsyncResults)
        {
            _router = router;
            _onBegin = onBegin;
            _onAsyncResults = onAsyncResults;
        }

        public override StepType Type
        {
            get { return StepType.Async; }
        }

        public override IObservable<TModel> ExecuteStream(TModel model)
        {
            return Observable.Create<TModel>(o =>
            {
                var disposables = new DisposableCollection();
                var stepResults = _onBegin(model);
                if (stepResults.ExecuteStep)
                {
                    var id = Guid.NewGuid();
                    var eventStreamDisposable =  _router
                        .GetEventObservable<AyncResultsEvent<TResults>>()
                        .Where((m, e, c) => e.Id == id)
                        .Observe((m, e, c) =>
                        {
                            _onAsyncResults(m, e.Result);
                            o.OnNext(model);
                        });
                    disposables.Add(eventStreamDisposable);

                    var observableStreamDispsoable = stepResults.ResultStream.Subscribe(
                        result => 
                        {
                            _router.PublishEvent(new AyncResultsEvent<TResults>(result, id));
                        },
                        exception =>
                        {
                            eventStreamDisposable.Dispose();
                            o.OnError(exception);
                        }, 
                        () =>
                        {
                            eventStreamDisposable.Dispose();
                            o.OnCompleted();
                        });
                    disposables.Add(observableStreamDispsoable);
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

        public override IObservable<TModel> ExecuteStream(TModel model)
        {
            throw new InvalidOperationException();
        }

        public override void Execute(TModel model)
        {
            _action(model);
        }
    }
}
#endif
