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
using Esp.Net.Meta;
using Esp.Net.ModelRouter;
using Esp.Net.Reactive;
using Esp.Net.Utils;

namespace Esp.Net   
{
    public partial class Router : IRouter
    {
        private readonly Dictionary<Guid, IModelEntry> _modelsById = new Dictionary<Guid, IModelEntry>();
        private readonly State _state = new State();
        private readonly RouterGuard _routerGuard;
        private readonly ModelsEventsObservations _modelsEventsObservations;

        public Router(IThreadGuard threadGuard)
        {
            Guard.Requires<ArgumentNullException>(threadGuard != null, "threadGuard can not be null");
            _routerGuard = new RouterGuard(_state, threadGuard);
            _modelsEventsObservations = new ModelsEventsObservations(threadGuard);
        }

        public IEventsObservationRegistrar EventsObservationRegistrar
        {
            get
            {
                return _modelsEventsObservations;
            }
        }

        public void RegisterModel<TModel>(Guid modelId, TModel model)
        {
            RegisterModel(modelId, model, null, null);
        }

        public void RegisterModel<TModel>(Guid modelId, TModel model, IPreEventProcessor<TModel> preEventProcessor)
        {
            Guard.Requires<ArgumentNullException>(preEventProcessor != null, "preEventProcessor can not be null");
            RegisterModel(modelId, model, preEventProcessor, null);
        }

        public void RegisterModel<TModel>(Guid modelId, TModel model, IPostEventProcessor<TModel> postEventProcessor)
        {
            Guard.Requires<ArgumentNullException>(postEventProcessor != null, "postEventProcessor can not be null");
            RegisterModel(modelId, model, null, postEventProcessor);
        }

        public void RegisterModel<TModel>(Guid modelId, TModel model, IPreEventProcessor<TModel> preEventProcessor, IPostEventProcessor<TModel> postEventProcessor)
        {
            Guard.Requires<ArgumentException>(modelId != Guid.Empty, "modelId can not be Guid.Empty");
            Guard.Requires<ArgumentNullException>(model != null, "model can not be null");
            _routerGuard.EnsureValid();
            Guard.Requires<ArgumentException>(!_modelsById.ContainsKey(modelId), "modelId {0} already registered", modelId);
            var entry = new ModelEntry<TModel>(
                modelId, 
                model, 
                preEventProcessor, 
                postEventProcessor, 
                _routerGuard,
                _modelsEventsObservations.CreateForModel(modelId)
            );
            _modelsById.Add(modelId, entry);
        }

        public void RemoveModel(Guid modelId)
        {
            //_routerGuard.EnsureValid();
            IModelEntry modelEntry;
            if (!_modelsById.TryGetValue(modelId, out modelEntry)) throw new ArgumentException(string.Format("Model with id {0} not registered", modelId));
            _modelsById.Remove(modelId);
            modelEntry.OnRemoved();
        }

        public void PublishEvent<TEvent>(Guid modelId, TEvent @event)
        {
            _routerGuard.EnsureValid();
            var modelEntry = _modelsById[modelId];
            modelEntry.Enqueue(@event);
            PurgeEventQueues();
        }

        public IModelObservable<TModel> GetModelObservable<TModel>(Guid modelId)
        {
            _routerGuard.EnsureValid();

            IModelEntry<TModel> entry = GetModelEntry<TModel>(modelId);
            return entry.GetModelObservable();
        }

        public IEventObservable<TModel, TEvent, IEventContext> GetEventObservable<TModel, TEvent>(Guid modelId, ObservationStage observationStage = ObservationStage.Normal)
        {
            _routerGuard.EnsureValid();
            IModelEntry<TModel> entry = GetModelEntry<TModel>(modelId);
            return entry.GetEventObservable<TEvent>(observationStage);
        }

        public IEventObservable<TModel, TBaseEvent, IEventContext> GetEventObservable<TModel, TSubEventType, TBaseEvent>(Guid modelId, ObservationStage observationStage = ObservationStage.Normal) where TSubEventType : TBaseEvent
        {
            _routerGuard.EnsureValid();
            IModelEntry<TModel> entry = GetModelEntry<TModel>(modelId);
            return entry.GetEventObservable<TSubEventType, TBaseEvent>(observationStage);
        }

        public IEventObservable<TModel, TBaseEvent, IEventContext> GetEventObservable<TModel, TBaseEvent>(Guid modelId, Type eventType, ObservationStage observationStage = ObservationStage.Normal)
        {
            _routerGuard.EnsureValid();
            IModelEntry<TModel> entry = GetModelEntry<TModel>(modelId);
            return entry.GetEventObservable<TBaseEvent>(eventType, observationStage);
        }

        public IRouter<TModel> CreateModelRouter<TModel>(Guid modelId)
        {
            return new ModelRouter<TModel>(modelId, this);
        }

        private void PurgeEventQueues()
        {
            if (_state.CurrentStatus == Status.Idle)
            {
                try
                {
                    IModelEntry modelEntry = GetNextModelEntryWithEvents();

                    while (modelEntry != null)
                    {
                        var changedModels = new HashSet<Guid>();
                        while (modelEntry != null)
                        {
                            _state.MoveToPreProcessing();
                            modelEntry.RunPreProcessor();
                            _state.MoveToEventDispatch();
                            modelEntry.PurgeEventQueue();
                            if(!changedModels.Contains(modelEntry.Id)) changedModels.Add(modelEntry.Id);
                            _state.MoveToPostProcessing();
                            modelEntry.RunPostProcessor();
                            modelEntry = GetNextModelEntryWithEvents();
                        }
                        _state.MoveToDispatchModelUpdates();
                        foreach (Guid id in changedModels)
                        {
                            _modelsById[id].DispatchModel();
                        }
                        modelEntry = GetNextModelEntryWithEvents();
                    }
                    _state.MoveToIdle();
                }
                catch (Exception ex)
                {
                    _state.MoveToHalted(ex);
                    throw;
                }
            }
        }

        private IModelEntry GetNextModelEntryWithEvents()
        {
            IModelEntry modelEntry = null;
            foreach (IModelEntry entry in _modelsById.Values)
            {
                if (entry.HadEvents)
                {
                    modelEntry = entry;
                    break;
                }
            }
            return modelEntry;
        }

        private IModelEntry<TModel> GetModelEntry<TModel>(Guid modelId)
        {
            IModelEntry entry;
            if (!_modelsById.TryGetValue(modelId, out entry))
            {
                throw new InvalidOperationException(string.Format("Model with id [{0}] isn't registered", modelId));
            }
            return (IModelEntry<TModel>)entry;
        }
    }
}
