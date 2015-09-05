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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Esp.Net.Meta
{
    internal class ModelEventObservations
    {
        private readonly Dictionary<Type, EventObservations> _eventObservations = new Dictionary<Type, EventObservations>();
        private readonly object _gate = new object();

        public void IncrementRegistration<TEventType>()
        {
            lock (_gate)
            {
                EventObservations eventObservations = GetEventObservations(typeof (TEventType));
                eventObservations.NumberOfObservers++;
            }
        }

        public void DecrementRegistration<TEventType>()
        {
            lock (_gate)
            {
                EventObservations eventObservations = GetEventObservations(typeof (TEventType));
                eventObservations.NumberOfObservers--;
            }
        }

        public int GetEventObservationCount<TEventType>()
        {
            lock (_gate)
                return GetEventObservationCount(typeof(TEventType));
        }

        public int GetEventObservationCount(Type eventType)
        {
            lock (_gate)
            {
                EventObservations eventObservations = GetEventObservations(eventType);
                return eventObservations.NumberOfObservers;
            }
        }

        public IList<EventObservations> GetEventObservations()
        {
            List<EventObservations> results;
            lock (_gate)
            {
                results = _eventObservations
                    .Values
                    .Select(v => new EventObservations(v.EventType, v.NumberOfObservers))
                    .ToList();
            }
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