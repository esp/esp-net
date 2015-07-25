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
    public class ModelsEventsObservations
    {
        private readonly Dictionary<Guid, ModelEventObservations> _modelRegistries;

        public ModelsEventsObservations()
        {
            _modelRegistries = new Dictionary<Guid, ModelEventObservations>(); 
        }

        internal void IncrementRegistration<TEvent>(Guid modelId)
        {
            ModelEventObservations eventObservations = GetEventRegistrations(modelId);
            eventObservations.IncrementRegistration<TEvent>();
        }

        internal void DecrementRegistration<TEvent>(Guid modelId)
        {
            ModelEventObservations eventObservations = GetEventRegistrations(modelId);
            eventObservations.DecrementRegistration<TEvent>();
        }

        public int GetEventObservationCount(Guid modelId, Type eventType)
        {
            ModelEventObservations eventObservations = GetEventRegistrations(modelId);
            return eventObservations[eventType].NumberOfObservers;
        }

        private ModelEventObservations GetEventRegistrations(Guid modelId)
        {
            ModelEventObservations eventObservations;
            if (!_modelRegistries.TryGetValue(modelId, out eventObservations))
            {
                eventObservations = new ModelEventObservations(modelId);
                _modelRegistries.Add(modelId, eventObservations);
            }
            return eventObservations;
        }

        internal IEventObservationRegistrar CreateForModel(Guid modelId)
        {
            return new ModelEventObservationRegistrar(modelId, this);
        }

        private class ModelEventObservationRegistrar : IEventObservationRegistrar
        {
            private readonly Guid _modelId;
            private readonly ModelsEventsObservations _parent;

            public ModelEventObservationRegistrar(Guid modelId, ModelsEventsObservations parent)
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