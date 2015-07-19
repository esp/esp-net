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

    public abstract class Step<TModel, TWorkflowContext> : DisposableBase
    {
        public abstract StepType Type { get; }

        public abstract IObservable<TModel> GetExecuteStream(TModel model, TWorkflowContext context);
        
        public abstract void Execute(TModel model, TWorkflowContext context);

        public Step<TModel, TWorkflowContext> Next { get; set; }
    }

    public class ObservableStep<TModel, TWorkflowContext, TResults> : Step<TModel, TWorkflowContext>
    {
        private readonly IRouter<TModel> _router;
        private readonly Func<TModel, TWorkflowContext, IObservable<TResults>> _observableFactory;
        private readonly Action<TModel, TWorkflowContext, TResults> _onAsyncResults;

        public ObservableStep(IRouter<TModel> router, Func<TModel, TWorkflowContext, IObservable<TResults>> observableFactory, Action<TModel, TWorkflowContext, TResults> onAsyncResults)
        {
            _router = router;
            _observableFactory = observableFactory;
            _onAsyncResults = onAsyncResults;
        }

        public override StepType Type
        {
            get { return StepType.Async; }
        }

        public override IObservable<TModel> GetExecuteStream(TModel model, TWorkflowContext context)
        {
            return Observable.Create<TModel>(o =>
            {
                var disposables = new CollectionDisposable();
                var observable = _observableFactory(model, context);

                // TODO
//                if(context.IsCanceled))
//                {
//                }

                var id = Guid.NewGuid();
                var eventStreamDisposable = _router
                    .GetEventObservable<AyncResultsEvent<TResults>>()
                    .Where((m, e, c) => e.Id == id)
                    .Observe(
                        (m, e) =>
                        {
                            _onAsyncResults(m, context, e.Result);
                            o.OnNext(model);
                        }
                    );
                disposables.Add(eventStreamDisposable);

                var observableStreamDispsoable = observable.Subscribe(
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
                    }
                );
                disposables.Add(observableStreamDispsoable);
                return disposables;
            });
        }

        public override void Execute(TModel model, TWorkflowContext context)
        {
            throw new InvalidOperationException();
        }
    }

    public class SyncStep<TModel, TWorkflowContext> : Step<TModel, TWorkflowContext>
    {
        private readonly Action<TModel, TWorkflowContext> _action;

        public SyncStep(Action<TModel, TWorkflowContext> action)
        {
            _action = action;
        }

        public override StepType Type
        {
            get { return StepType.Sync; }
        }

        public override IObservable<TModel> GetExecuteStream(TModel model, TWorkflowContext context)
        {
            throw new InvalidOperationException();
        }

        public override void Execute(TModel model, TWorkflowContext context)
        {
            _action(model, context);
        }
    }
}
#endif
