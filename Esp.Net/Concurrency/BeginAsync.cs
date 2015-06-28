using System;
using Esp.Net.Model;
using Esp.Net.Reactive;
using Esp.Net.RxBridge;

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
    }
}