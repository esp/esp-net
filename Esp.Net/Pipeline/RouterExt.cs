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

        public static IEventObservable<IEventContext<TModel, AsyncResultsEvent<TResults>>> RunAsyncOperation<TModel, TEvent, TResults>(this IEventObservable<IEventContext<TModel, TEvent>> source, Func<IEventContext<TModel, TEvent>, IObservable<TResults>> asyncStream)
        {
            return EventObservable.Create<IEventContext<TModel, AsyncResultsEvent<TResults>>>(o =>
            {
                return () =>
                {

                };
            });
        }
    }
}