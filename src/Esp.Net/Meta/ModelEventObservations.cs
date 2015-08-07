using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Esp.Net.Meta
{
    internal class ModelEventObservations
    {
        private readonly Dictionary<Type, EventObservations> _eventObservations = new Dictionary<Type, EventObservations>();

        public void IncrementRegistration<TEventType>()
        {
            EventObservations eventObservations = GetEventObservations(typeof(TEventType));
            eventObservations.NumberOfObservers++;
        }

        public void DecrementRegistration<TEventType>()
        {
            EventObservations eventObservations = GetEventObservations(typeof(TEventType));
            eventObservations.NumberOfObservers--;
        }

        public int GetEventObservationCount<TEventType>()
        {
            return GetEventObservationCount(typeof(TEventType));
        }

        public int GetEventObservationCount(Type eventType)
        {
            EventObservations eventObservations = GetEventObservations(eventType);
            return eventObservations.NumberOfObservers;
        }

        public IList<EventObservations> GetEventObservations()
        {
            var results = _eventObservations
                .Values
                .Select(v => new EventObservations(v.EventType, v.NumberOfObservers))
                .ToList();
            return new ReadOnlyCollection<EventObservations>(results);
        }

        private EventObservations GetEventObservations(Type eventType)
        {
            EventObservations eventObservations;
            if (!_eventObservations.TryGetValue(eventType, out eventObservations))
            {
                eventObservations = new EventObservations(eventType);
                _eventObservations.Add(eventType, eventObservations);
            }
            return eventObservations;
        }
    }
}