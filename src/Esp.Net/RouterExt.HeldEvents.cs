#region copyright
// Copyright 2015 Dev Shop Limited
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

using System;
using System.Collections.Generic;
using System.Reflection;
using Esp.Net.Utils;

namespace Esp.Net
{
    public static partial class RouterExt
    {
        private static readonly MethodInfo GetEventObservableMethodInfo = ReflectionHelper.GetGenericMethodByArgumentCount(typeof(RouterExt), "GetEventObservable", 2, 2);

        public static IEventObservable<TBaseEvent, IEventContext, TModel> GetEventObservable<TEvent, TBaseEvent, TModel>(this IRouter router, Guid modelId, IEventHoldingStrategy<TEvent, TBaseEvent, TModel> strategy)
            where TEvent : TBaseEvent, IIdentifiableEvent
            where TModel : IHeldEventStore
        {
            var modelRouter = new Router<TModel>(modelId, router);
            return modelRouter.GetEventObservable(strategy);
        }

        public static IEventObservable<TBaseEvent, IEventContext, TModel> GetEventObservable<TEvent, TBaseEvent, TModel>(this IRouter<TModel> router, IEventHoldingStrategy<TEvent, TBaseEvent, TModel> strategy)
            where TEvent : TBaseEvent, IIdentifiableEvent
            where TModel : IHeldEventStore
        {
            object[] parameters = new object[] { router, strategy };
            return EventObservable.Create<TBaseEvent, IEventContext, TModel>(
                o =>
                {
                    var getEventStreamMethod = GetEventObservableMethodInfo.MakeGenericMethod(typeof(TEvent), typeof(TModel));
                    dynamic observable = getEventStreamMethod.Invoke(null, parameters);
                    return (IDisposable)observable.Observe(o);                    
                }
            );
        }

        public static IEventObservable<TEvent, IEventContext, TModel> GetEventObservable<TEvent, TModel>(this IRouter router, Guid modelId, IEventHoldingStrategy<TEvent, TModel> strategy)
            where TEvent : IIdentifiableEvent
            where TModel : IHeldEventStore
        {
            var modelRouter = new Router<TModel>(modelId, router);
            return modelRouter.GetEventObservable(strategy);
        }

        public static IEventObservable<TEvent, IEventContext, TModel> GetEventObservable<TEvent, TModel>(this IRouter<TModel> router, IEventHoldingStrategy<TEvent, TModel> strategy)
            where TEvent : IIdentifiableEvent
            where TModel : IHeldEventStore
        {
            return EventObservable.Create<TEvent, IEventContext, TModel>(
                o =>
                {
                    var heldEvents = new Dictionary<Guid, HeldEventData<TEvent>>();
                    var releasedEvents = new HashSet<Guid>();
                    var disposables = new CollectionDisposable();
                    disposables.Add(router.GetEventObservable<TEvent>(ObservationStage.Preview).Observe((e, c, m) =>
                    {
                        // Have we already re-published this event? If so we don't want to hold it again.
                        if (releasedEvents.Contains(e.Id))
                        {
                            releasedEvents.Remove(e.Id);
                            o.OnNext(e, c, m);
                        }
                        else
                        {
                            var shouldHoldEvent = strategy.ShouldHold(e, c, m);
                            if (shouldHoldEvent)
                            {
                                // Cancel the event so no other observers will receive it.
                                c.Cancel(); 
                                // Model that we've cancelled it, other code can now reflect the newly modelled values.
                                // That is other code needs to determine what to do with these held events on the model. 
                                // When it figures that out it should raise a HeldEventActionEvent so we can proceed here.
                                IEventDescription eventDescription = strategy.GetEventDescription(e, m);
                                m.AddHeldEventDescription(eventDescription);
                                // finally we hold the event locally
                                heldEvents.Add(e.Id, new HeldEventData<TEvent>(eventDescription, e));
                            }
                        }
                    }));
                    disposables.Add(router.GetEventObservable<HeldEventActionEvent>().Observe((e, c, m) =>
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