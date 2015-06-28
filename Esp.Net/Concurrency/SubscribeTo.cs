using System;
using Esp.Net.Model;
using Esp.Net.Reactive;
using Esp.Net.RxBridge;

#if ESP_EXPERIMENTAL
namespace Esp.Net.Concurrency
{
    public static class SubscribeToRouterExt
    {
        /// <summary>
        /// Subscribes to an IObservable event stream and waits for result events. 
        /// Each time eventObservable yields the event will be published to the given router. 
        /// This allows others to process it via the staged event workflow (which could involve cancelation). 
        /// TResultEvent can be applied to he model by observing the returned IEventObservable.
        /// <remarks>
        /// It's important to note that SubscribeTo must publish TResultEvent to the provide model to enable the entire event workflow to take place. 
        /// The model most move through it's correct stage to guarantee a deterministic application of model state based on events
        /// </remarks>
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <typeparam name="TEvent"></typeparam>
        /// <typeparam name="TResultEvent"></typeparam>
        /// <param name="eventObservable">the source event stream</param>
        /// <param name="eventStreamFactory">this factory returns an IObservable which will be subscribed to for each yield of eventObservable</param>
        /// <param name="router">the router to publish the results back to (which allows TResultEvent to take part in the full event workflow)</param>
        /// <returns></returns>
        public static IEventObservable<TModel, TResultEvent, IEventContext> SubscribeTo<TModel, TEvent, TResultEvent>(
            this IEventObservable<TModel, TEvent, IEventContext> eventObservable,
            Func<TModel, TEvent, IEventContext, IObservable<TResultEvent>> eventStreamFactory,
            IRouter<TModel> router)
            where TResultEvent : IdentifiableEvent
        {
            return EventObservable.Create<TModel, TResultEvent, IEventContext>(o =>
            {
                var disposables = new DisposableCollection();
                disposables.Add(eventObservable.Observe((initialModel, initialEvent, initialContext) =>
                {
                    IObservable<TResultEvent> asyncStream = eventStreamFactory(initialModel, initialEvent, initialContext);

                    // TODO as asyncStream completes or errors we need to remove it from disposables 
                    disposables.Add(asyncStream.Subscribe(
                        results =>
                        {
                            disposables.Add(router
                                .GetEventObservable<TResultEvent>()
                                .Where((resultsModel, resultsEvent, resultsContext) => resultsEvent.Id == results.Id)
                                .Observe(o)
                            );
                            router.PublishEvent(results);
                        }, 
                        ex =>
                        {
                        
                        },
                        () =>
                        {
                            
                        }
                    ));
                }));
                return disposables;
            });
        }
    }
}
#endif