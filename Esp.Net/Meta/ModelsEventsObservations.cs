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
using Esp.Net.Utils;

namespace Esp.Net.Meta
{
    internal class ModelsEventsObservations : IEventsObservationRegistrar
    {
        private readonly IThreadGuard _threadGuard;
        private readonly Dictionary<Guid, ModelEventObservations> _modelRegistries;

        public ModelsEventsObservations(IThreadGuard threadGuard)
        {
            _threadGuard = threadGuard;
            _modelRegistries = new Dictionary<Guid, ModelEventObservations>();
        }

        public void IncrementRegistration<TEvent>(Guid modelId)
        {
            ModelEventObservations eventObservations = GetEventRegistrations(modelId);
            eventObservations.IncrementRegistration<TEvent>();
        }

        public void DecrementRegistration<TEvent>(Guid modelId)
        {
            ModelEventObservations eventObservations = GetEventRegistrations(modelId);
            eventObservations.DecrementRegistration<TEvent>();
        }

        int IEventsObservationRegistrar.GetEventObservationCount<TEventType>(Guid modelId)
        {
            Guard.Requires<InvalidOperationException>(_threadGuard.CheckAccess(), "Invalid thread access");
            ModelEventObservations eventObservations = GetEventRegistrations(modelId);
            return eventObservations.GetEventObservationCount<TEventType>();
        }

        int IEventsObservationRegistrar.GetEventObservationCount(Guid modelId, Type eventType)
        {
            Guard.Requires<InvalidOperationException>(_threadGuard.CheckAccess(), "Invalid thread access");
            ModelEventObservations eventObservations = GetEventRegistrations(modelId);
            return eventObservations.GetEventObservationCount(eventType);
        }

        IList<EventObservations> IEventsObservationRegistrar.GetEventObservations(Guid modelId)
        {
            Guard.Requires<InvalidOperationException>(_threadGuard.CheckAccess(), "Invalid thread access");
            ModelEventObservations eventObservations = GetEventRegistrations(modelId);
            return eventObservations.GetEventObservations();
        }

        private ModelEventObservations GetEventRegistrations(Guid modelId)
        {
            ModelEventObservations eventObservations;
            if (!_modelRegistries.TryGetValue(modelId, out eventObservations))
            {
                eventObservations = new ModelEventObservations();
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