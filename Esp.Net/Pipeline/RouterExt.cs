using System;
using Esp.Net.Model;
using Esp.Net.Reactive;
using Esp.Net.RxBridge;

namespace Esp.Net.Pipeline
{
    public static class RouterExt
    {
        public static PipelineBuilder<TModel> ConfigurePipeline<TModel>(this IRouter<TModel> router)
        {
            return new PipelineBuilder<TModel>(router);
        }

        public static IEventObservable<TModel, AsyncResultsEvent<TResults>, IEventContext<TModel>> BeginAcync<TModel, TEvent, TResults>(
            this IEventObservable<TModel, TEvent, IEventContext<TModel>> source,
            Func<TModel, TEvent, IEventContext<TModel>, IObservable<TResults>> asyncStreamFactory)
        {
            return EventObservable.Create<TModel, AsyncResultsEvent<TResults>, IEventContext<TModel>>(o =>
            {
                var disposables = new DisposableCollection();
                disposables.Add(source.Observe((m, e, c) =>
                {
                    var asyncStream = asyncStreamFactory(m, e, c);
                    disposables.Add(asyncStream.Subscribe(results =>
                    {
                        disposables.Add(c.Router.SubmitAsyncResults(results).Observe(o));
                    }));
                }));
                return disposables;
            });
        }
    }
}