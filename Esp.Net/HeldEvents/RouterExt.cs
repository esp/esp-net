using System;
using System.Collections.Generic;
using Esp.Net.Model;
using Esp.Net.Reactive;

#if ESP_EXPERIMENTAL
namespace Esp.Net.HeldEvents
{
    public static class RouterExt
    {
        public static IEventObservable<TModel, TEvent, IEventContext> GetEventObservable<TModel, TEvent>(
            this IRouter<TModel> router, 
            IEventHoldingStrategy<TModel, TEvent, IEventContext> strategy
        )
            where TEvent : IIdentifiableEvent
            where TModel : IHeldEventStore
        {
            return EventObservable.Create<TModel, TEvent, IEventContext>(
                o =>
                {
                    var heldEvents = new Dictionary<Guid, TEvent>();
                    var releasedEvents = new HashSet<Guid>();
                    var disposables = new DisposableCollection();
                    disposables.Add(router.GetEventObservable<TEvent>(ObservationStage.Preview).Observe((m, e, c) =>
                    {
                        // Have we already re-published this event? If so we don't want to hold it again.
                        if (releasedEvents.Contains(e.Id))
                        {
                            releasedEvents.Remove(e.Id);
                        }
                        else
                        {
                            var shouldHoldEvent = strategy.ShouldHold(m, e, c);
                            if (shouldHoldEvent)
                            {
                                // Cancel the event so no other observers will receive it.
                                c.Cancel(); 
                                // Model that we've canceled it, other code can now reflect the newly modeled values.
                                // That is other code needs to determine what to do with these held events on the model. 
                                // When it figures that out it should raise a HeldEventActionEvent so we can proceed here.
                                var eventDescription = strategy.GetEventDescription(m, e);
                                m.AddHeldEventDescription(e.Id, eventDescription);
                                // finally we hold the event locally
                                heldEvents.Add(e.Id, e);
                            }
                        }
                    }));
                    disposables.Add(router.GetEventObservable<HeldEventActionEvent>().Observe((m, e, c) =>
                    {
                        // We're received an event to clean up.
                        var heldEvent = heldEvents[e.EventId];
                        heldEvents.Remove(e.EventId);
                        m.RemoveHeldEventDescription(e.EventId);
                        if (e.Action == HeldEventAction.Release)
                        {
                            // Temporarly store the evnet we're republishing so we don't re-hold it
                            releasedEvents.Add(e.EventId);
                            router.PublishEvent(heldEvent);
                        }
                    }));
                    return disposables;
                }
            );
        }
    }
}
#endif