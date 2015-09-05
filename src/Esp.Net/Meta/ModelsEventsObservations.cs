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

namespace Esp.Net.Meta
{
    internal class ModelsEventsObservations : IEventsObservationRegistrar
    {
        private readonly Dictionary<object, ModelEventObservations> _modelRegistries;
        private readonly object _gate = new object();

        public ModelsEventsObservations()
        {
            _modelRegistries = new Dictionary<object, ModelEventObservations>();
        }

        public void IncrementRegistration<TEvent>(object modelId)
        {
            ModelEventObservations eventObservations = GetEventRegistrations(modelId);
            eventObservations.IncrementRegistration<TEvent>();
        }

        public void DecrementRegistration<TEvent>(object modelId)
        {
            ModelEventObservations eventObservations = GetEventRegistrations(modelId);
            eventObservations.DecrementRegistration<TEvent>();
        }

        int IEventsObservationRegistrar.GetEventObservationCount<TEventType>(object modelId)
        {
            ModelEventObservations eventObservations = GetEventRegistrations(modelId);
            return eventObservations.GetEventObservationCount<TEventType>();
        }

        int IEventsObservationRegistrar.GetEventObservationCount(object modelId, Type eventType)
        {
            ModelEventObservations eventObservations = GetEventRegistrations(modelId);
            return eventObservations.GetEventObservationCount(eventType);
        }

        IList<EventObservations> IEventsObservationRegistrar.GetEventObservations(object modelId)
        {
            ModelEventObservations eventObservations = GetEventRegistrations(modelId);
            return eventObservations.GetEventObservations();
        }

        private ModelEventObservations GetEventRegistrations(object modelId)
        {
            ModelEventObservations eventObservations;
            lock (_gate)
            {
                if (!_modelRegistries.TryGetValue(modelId, out eventObservations))
                {
                    eventObservations = new ModelEventObservations();
                    _modelRegistries.Add(modelId, eventObservations);
                }
            }
            return eventObservations;
        }

        internal IEventObservationRegistrar CreateForModel(object modelId)
        {
            return new ModelEventObservationRegistrar(modelId, this);
        }

        private class ModelEventObservationRegistrar : IEventObservationRegistrar
        {
            private readonly object _modelId;
            private readonly ModelsEventsObservations _parent;

            public ModelEventObservationRegistrar(object modelId, ModelsEventsObservations parent)
            {
                _modelId = modelId;
                _parent = parent;
            }

            public void IncrementRegistration<TEvent>()
            {
                _parent.IncrementRegistration<TEvent>(_modelId);
            }

            public void DecrementRegistration<TEvent>()
            {
                _parent.DecrementRegistration<TEvent>(_modelId);
            }
        }
    }
}