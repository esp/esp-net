#if ESP_EXPERIMENTAL
using System;
using System.Reactive.Linq;
using Esp.Net.Disposables;
using Esp.Net.Reactive;

namespace Esp.Net.Plugins.Workflow
{
    public enum StepType
    {
        Async,
        Sync
    }

    internal abstract class Step<TModel, TWorkflowContext> : DisposableBase
    {
        public abstract StepType Type { get; }

        public abstract IObservable<TModel> GetExecuteStream(Guid modelId, TModel model, TWorkflowContext context);
        
        public abstract void Execute(TModel model, TWorkflowContext context);

        public Step<TModel, TWorkflowContext> Next { get; set; }
    }

    internal class ObservableStep<TModel, TWorkflowContext, TResults> : Step<TModel, TWorkflowContext>
    {
        private readonly IRouter _router;
        private readonly Func<TModel, TWorkflowContext, IObservable<TResults>> _observableFactory;
        private readonly Action<TModel, TWorkflowContext, TResults> _onAsyncResults;

        public ObservableStep(IRouter router, Func<TModel, TWorkflowContext, IObservable<TResults>> observableFactory, Action<TModel, TWorkflowContext, TResults> onAsyncResults)
        {
            _router = router;
            _observableFactory = observableFactory;
            _onAsyncResults = onAsyncResults;
        }

        public override StepType Type
        {
            get { return StepType.Async; }
        }

        public override IObservable<TModel> GetExecuteStream(Guid modelId, TModel model, TWorkflowContext context)
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
                    .GetEventObservable<TModel, AyncResultsEvent<TResults>>(modelId)
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
                        _router.PublishEvent(modelId, new AyncResultsEvent<TResults>(result, id));
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

    internal class SyncStep<TModel, TWorkflowContext> : Step<TModel, TWorkflowContext>
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

        public override IObservable<TModel> GetExecuteStream(Guid modelId, TModel model, TWorkflowContext context)
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
