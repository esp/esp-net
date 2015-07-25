using System;
using System.Collections.Generic;

namespace Esp.Net.Meta
{
    public class ModelEventObservations
    {
        private readonly Dictionary<Type, EventObservations> _eventObservations = new Dictionary<Type, EventObservations>();

        public ModelEventObservations(Guid modelId)
        {
            ModelId = modelId;
        }

        public Guid ModelId { get; private set; }

        public EventObservations this[Type eventType]
        {
            get { return _eventObservations[eventType];  }
        }

        internal void IncrementRegistration<TEventType>()
        {
            EventObservations eventObservations = GetEventObservations(typeof(TEventType));
            eventObservations.NumberOfObservers++;
        }

        internal void DecrementRegistration<TEventType>()
        {
            EventObservations eventObservations = GetEventObservations(typeof(TEventType));
            eventObservations.NumberOfObservers--;
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