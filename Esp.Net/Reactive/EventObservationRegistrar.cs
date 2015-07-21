using System;
using System.Collections.Generic;

namespace Esp.Net.Reactive
{
    public class EventObservationRegistrar
    {
        private readonly Dictionary<Guid, Dictionary<Type, int>> _modelRegistries;

        public EventObservationRegistrar()
        {
            _modelRegistries = new Dictionary<Guid, Dictionary<Type, int>>(); 
        }

        internal void AddRegistration(Guid modelId, Type eventType)
        {
            Dictionary<Type, int> eventRegistrations = GetEventRegistrations(modelId);
            if (eventRegistrations.ContainsKey(eventType))
            {
                eventRegistrations[eventType]++;
            }
            else
            {
                eventRegistrations[eventType] = 1;
            }
        }

        internal void RemoveRegistration(Guid modelId, Type eventType)
        {
            Dictionary<Type, int> eventRegistrations = GetEventRegistrations(modelId);
            eventRegistrations[eventType]--;
        }

        public int GetEventObservationCount(Guid modelId, Type eventType)
        {
            Dictionary<Type, int> eventRegistrations = GetEventRegistrations(modelId);
            return eventRegistrations[eventType];
        }

        private Dictionary<Type, int> GetEventRegistrations(Guid modelId)
        {
            Dictionary<Type, int> eventRegistrations;
            if (!_modelRegistries.TryGetValue(modelId, out eventRegistrations))
            {
                eventRegistrations = new Dictionary<Type, int>();
                _modelRegistries.Add(modelId, eventRegistrations);
            }
            return eventRegistrations;
        }
    }
}