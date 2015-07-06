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

    public abstract class Step<TModel, TPipelineContext> : DisposableBase
    {
        public abstract StepType Type { get; }

        public abstract IObservable<TModel> GetExecuteStream(TModel model, TPipelineContext context);
        
        public abstract void Execute(TModel model, TPipelineContext context);

        public Step<TModel, TPipelineContext> Next { get; set; }
    }

    public class ObservableStep<TModel, TPipelineContext, TResults> : Step<TModel, TPipelineContext>
    {
        private readonly IRouter<TModel> _router;
        private readonly Func<TModel, TPipelineContext, IObservable<TResults>> _observableFactory;
        private readonly Action<TModel, TResults> _onAsyncResults;

        public ObservableStep(IRouter<TModel> router, Func<TModel, TPipelineContext, IObservable<TResults>> observableFactory, Action<TModel, TResults> onAsyncResults)
        {
            _router = router;
            _observableFactory = observableFactory;
            _onAsyncResults = onAsyncResults;
        }

        public override StepType Type
        {
            get { return StepType.Async; }
        }

        public override IObservable<TModel> GetExecuteStream(TModel model, TPipelineContext context)
        {
            return Observable.Create<TModel>(o =>
            {
                var disposables = new DisposableCollection();
                var observable = _observableFactory(model, context);

//                if(context.IsCanceled))
//                {
//                }

                var id = Guid.NewGuid();
                var eventStreamDisposable = _router
                    .GetEventObservable<AyncResultsEvent<TResults>>()
                    .Where((m, e, c) => e.Id == id)
                    .Observe(
                        (m, e, c) =>
                        {
                            _onAsyncResults(m, e.Result);
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

        public override void Execute(TModel model, TPipelineContext context)
        {
            throw new InvalidOperationException();
        }
    }

    public class SyncStep<TModel, TPipelineContext> : Step<TModel, TPipelineContext>
    {
        private readonly Action<TModel, TPipelineContext> _action;

        public SyncStep(Action<TModel, TPipelineContext> action)
        {
            _action = action;
        }

        public override StepType Type
        {
            get { return StepType.Sync; }
        }

        public override IObservable<TModel> GetExecuteStream(TModel model, TPipelineContext context)
        {
            throw new InvalidOperationException();
        }

        public override void Execute(TModel model, TPipelineContext context)
        {
            _action(model, context);
        }
    }
}
#endif
