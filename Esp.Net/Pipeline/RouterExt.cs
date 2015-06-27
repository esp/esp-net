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

        public static IEventObservable<TModel, AsyncResultsEvent<TResults>, IEventContext> RunAsyncOperation<TModel, TEvent, TResults>(
            this IEventObservable<TModel, TEvent, IEventContext> source,
            Func<TModel, TEvent, IEventContext, IObservable<TResults>> asyncStream)
        {
            return EventObservable.Create<TModel, AsyncResultsEvent<TResults>, IEventContext>(o =>
            {
                
                return () =>
                {

                };
            });
        }
    }
}