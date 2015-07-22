#region copyright
// Copyright 2015 Keith Woods
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

#if ESP_EXPERIMENTAL
using System;
using System.Collections.Generic;
using System.Reflection;
using Esp.Net.Disposables;
using Esp.Net.Reactive;
using Esp.Net.Utils;

namespace Esp.Net.Plugins.HeldEvents
{
    public static class RouterExt
    {
        private static readonly MethodInfo GetEventObservableMethodInfo = ReflectionHelper.GetGenericMethodByArgumentCount(typeof(RouterExt), "GetEventObservable", 2, 3);

        public static IEventObservable<TModel, TBaseEvent, IEventContext> GetEventObservable<TModel, TEvent, TBaseEvent>(
            this IRouter router,
            Guid modelId,
            IEventHoldingStrategy<TModel, TEvent, TBaseEvent> strategy
        )
            where TEvent : TBaseEvent, IIdentifiableEvent
            where TModel : IHeldEventStore
        {
            object[] parameters = new object[] { router, modelId, strategy };
            return EventObservable.Create<TModel, TBaseEvent, IEventContext>(
                o =>
                {
                    var getEventStreamMethod = GetEventObservableMethodInfo.MakeGenericMethod(typeof(TModel), typeof(TEvent));
                    dynamic observable = getEventStreamMethod.Invoke(null, parameters);
                    return (IDisposable)observable.Observe(o);                    
                }
            );
        }

        public static IEventObservable<TModel, TEvent, IEventContext> GetEventObservable<TModel, TEvent>(
            this IRouter router,
            Guid modelId,
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
                    disposables.Add(router.GetEventObservable<TModel, TEvent>(modelId, ObservationStage.Preview).Observe((m, e, c) =>
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
                    disposables.Add(router.GetEventObservable<TModel, HeldEventActionEvent>(modelId).Observe((m, e, c) =>
                    {
                        HeldEventData<TEvent> heldEventData;
                        // Since we're listening to a pipe of all events we need to filter out anything we don't know about.
                        if (heldEvents.TryGetValue(e.EventId, out heldEventData))
                        {
                            // We're received an event to clean up.
                            heldEvents.Remove(e.EventId);
                            m.RemoveHeldEventDescription(heldEventData.EventDescription);
                            if (e.Action == HeldEventAction.Release)
                            {
                                // Temporarily store the event we're republishing so we don't re-hold it
                                releasedEvents.Add(e.EventId);
                                router.PublishEvent(modelId, heldEventData.Event);
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