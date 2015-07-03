#if ESP_EXPERIMENTAL

using System;
using Esp.Net.Model;
using Esp.Net.Reactive;
using System.Reactive.Linq;

namespace Esp.Net.Concurrency
{
    public static class BeginAsyncRouterExt
    {
        public static IEventObservable<TModel, AsyncResultsEvent<TResults>, IEventContext> BeginAcync<TModel, TEvent, TResults>(
            this IEventObservable<TModel, TEvent, IEventContext> source,
            Func<TModel, TEvent, IEventContext, IObservable<TResults>> asyncStreamFactory,
            IRouter<TModel> router)
        {
            return EventObservable.Create<TModel, AsyncResultsEvent<TResults>, IEventContext>(o =>
            {
                var disposables = new DisposableCollection();
                disposables.Add(source.Observe((m, e, c) =>
                {
                    var asyncStream = asyncStreamFactory(m, e, c);
                    disposables.Add(asyncStream.Subscribe(results =>
                    {
                        disposables.Add(router.SubmitAsyncResults(results).Observe(o));
                    }));
                }));
                return disposables;
            });
        }

        internal static IEventObservable<TModel, AsyncResultsEvent<TResults>, IEventContext> SubmitAsyncResults<TModel, TResults>(this IRouter<TModel> router, TResults results)
        {
            return EventObservable.Create<TModel, AsyncResultsEvent<TResults>, IEventContext>(
                o =>
                {
                    var asyncEventId = Guid.NewGuid();
                    IDisposable disposable = router.GetEventObservable<AsyncResultsEvent<TResults>>()
                        .Where((m, e, c) => e.Id == asyncEventId)
                        .Observe(o);
                    var @event = new AsyncResultsEvent<TResults>(results, asyncEventId);
                    router.PublishEvent(@event);
                    return disposable;
                }
            );
        }
    }
}
#endif