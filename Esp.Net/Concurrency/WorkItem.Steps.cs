using System;
using Esp.Net.Model;
using Esp.Net.Reactive;
using Esp.Net.RxBridge;

#if ESP_EXPERIMENTAL
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

        public abstract IObservable<TModel> GetExecuteStream(TModel model);
        
        public abstract void Execute(TModel model);

        public Step<TModel> Next { get; set; }
    }

    public class ObservableStep<TModel, TResults> : Step<TModel>
    {
        private readonly IRouter<TModel> _router;
        private readonly Func<TModel, IObservable<TResults>> _observableFactory;
        private readonly Action<TModel, TResults> _onAsyncResults;

        public ObservableStep(IRouter<TModel> router, Func<TModel, IObservable<TResults>> observableFactory, Action<TModel, TResults> onAsyncResults)
        {
            _router = router;
            _observableFactory = observableFactory;
            _onAsyncResults = onAsyncResults;
        }

        public override StepType Type
        {
            get { return StepType.Async; }
        }

        public override IObservable<TModel> GetExecuteStream(TModel model)
        {
            return EspObservable.Create<TModel>(o =>
            {
                var disposables = new DisposableCollection();
                var observable = _observableFactory(model);
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

        public override void Execute(TModel model)
        {
            throw new InvalidOperationException();
        }
    }

    public class SyncStep<TModel> : Step<TModel>
    {
        private readonly Action<TModel> _action;

        public SyncStep(Action<TModel> action)
        {
            _action = action;
        }

        public override StepType Type
        {
            get { return StepType.Sync; }
        }

        public override IObservable<TModel> GetExecuteStream(TModel model)
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
