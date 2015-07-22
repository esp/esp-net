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
using Esp.Net.Reactive;

namespace Esp.Net
{
    public class EventObservationRegistrar
    {
        private readonly Dictionary<Guid, Dictionary<Type, int>> _modelRegistries;

        public EventObservationRegistrar()
        {
            _modelRegistries = new Dictionary<Guid, Dictionary<Type, int>>(); 
        }

        internal void IncrementRegistration(Guid modelId, Type eventType)
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

        internal void DecrementRegistration(Guid modelId, Type eventType)
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

        internal IEventObservationRegistrar CreateForModel(Guid modelId)
        {
            return new ModelEventObservationRegistrar(modelId, this);
        }

        private class ModelEventObservationRegistrar : IEventObservationRegistrar
        {
            private readonly Guid _modelId;
            private readonly EventObservationRegistrar _parent;

            public ModelEventObservationRegistrar(Guid modelId, EventObservationRegistrar parent)
            {
                _modelId = modelId;
                _parent = parent;
            }

            public void IncrementRegistration<TEvent>()
            {
                _parent.IncrementRegistration(_modelId, typeof(TEvent));
            }

            public void DecrementRegistration<TEvent>()
            {
                _parent.DecrementRegistration(_modelId, typeof(TEvent));
            }
        }
    }
}