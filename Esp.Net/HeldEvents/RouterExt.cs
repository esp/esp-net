using System;
using System.Collections.Generic;
using System.Reflection;
using Esp.Net.Model;
using Esp.Net.Reactive;

#if ESP_EXPERIMENTAL
namespace Esp.Net.HeldEvents
{
    public static class RouterExt
    {
        private static readonly MethodInfo GetEventObservableMethodInfo = ReflectionHelper.GetGenericMethodByArgumentCount(typeof(RouterExt), "GetEventObservable", 2, 2);

        public static IEventObservable<TModel, TBaseEvent, IEventContext> GetEventObservable<TModel, TEvent, TBaseEvent>(
            this IRouter<TModel> router,
            IEventHoldingStrategy<TModel, TEvent, TBaseEvent> strategy
        )
            where TEvent : TBaseEvent, IIdentifiableEvent
            where TModel : IHeldEventStore
        {
            return EventObservable.Create<TModel, TBaseEvent, IEventContext>(
                o =>
                {
                    var getEventStreamMethod = GetEventObservableMethodInfo.MakeGenericMethod(typeof(TModel), typeof(TEvent));
                    dynamic observable = getEventStreamMethod.Invoke(null, new object[] { router, strategy });
                    return (IDisposable)observable.Observe(o);                    
                }
            );
        }

        public static IEventObservable<TModel, TEvent, IEventContext> GetEventObservable<TModel, TEvent>(
            this IRouter<TModel> router,
            IEventHoldingStrategy<TModel, TEvent> strategy
        )
            where TEvent : IIdentifiableEvent
            where TModel : IHeldEventStore
        {
            return EventObservable.Create<TModel, TEvent, IEventContext>(
                o =>
                {
                    var heldEvents = new Dictionary<Guid, HeldEventData<TEvent>>();
                    var releasedEvents = new HashSet<Guid>();
                    var disposables = new CollectionDisposable();
                    disposables.Add(router.GetEventObservable<TEvent>(ObservationStage.Preview).Observe((m, e, c) =>
                    {
                        // Have we already re-published this event? If so we don't want to hold it again.
                        if (releasedEvents.Contains(e.Id))
                        {
                            releasedEvents.Remove(e.Id);
                            o.OnNext(m, e, c);
                        }
                        else
                        {
                            var shouldHoldEvent = strategy.ShouldHold(m, e, c);
                            if (shouldHoldEvent)
                            {
                                // Cancel the event so no other observers will receive it.
                                c.Cancel(); 
                                // Model that we've cancelled it, other code can now reflect the newly modelled values.
                                // That is other code needs to determine what to do with these held events on the model. 
                                // When it figures that out it should raise a HeldEventActionEvent so we can proceed here.
                                IEventDescription eventDescription = strategy.GetEventDescription(m, e);
                                m.AddHeldEventDescription(eventDescription);
                                // finally we hold the event locally
                                heldEvents.Add(e.Id, new HeldEventData<TEvent>(eventDescription, e));
                            }
                        }
                    }));
                    disposables.Add(router.GetEventObservable<HeldEventActionEvent>().Observe((m, e, c) =>
                    {
                        // Since we're listening to a pipe of all events we need to filter out anything we don't know about.
                        if (heldEvents.ContainsKey(e.EventId))
                        {
                            // We're received an event to clean up.
                            var heldEventData = heldEvents[e.EventId];
                            heldEvents.Remove(e.EventId);
                            m.RemoveHeldEventDescription(heldEventData.EventDescription);
                            if (e.Action == HeldEventAction.Release)
                            {
                                // Temporarily store the event we're republishing so we don't re-hold it
                                releasedEvents.Add(e.EventId);
                                router.PublishEvent(heldEventData.Event);
                            }
                        }
                    }));
                    return disposables;
                }
            );
        }

        private class HeldEventData<TEvent>
        {
            public HeldEventData(IEventDescription eventDescription, TEvent @event)
            {
                EventDescription = eventDescription;
                Event = @event;
            }

            public IEventDescription EventDescription { get; private set; }

            public TEvent Event { get; private set; }
        }
    }
}
#endif