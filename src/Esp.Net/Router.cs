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
using System.Linq;
using System.Reflection;
using Esp.Net.Meta;
using Esp.Net.Utils;

namespace Esp.Net   
{
    public partial class Router : IRouter
    {
        private static readonly MethodInfo PublishEventMethodInfo = ReflectionHelper.GetGenericMethodByArgumentCount(typeof(Router), "PublishEvent", 1, 2);
        private static readonly MethodInfo ExecuteEventMethodInfo = ReflectionHelper.GetGenericMethodByArgumentCount(typeof(Router), "ExecuteEvent", 1, 2);
        private static readonly MethodInfo BroadcastEventMethodInfo = ReflectionHelper.GetGenericMethodByArgumentCount(typeof(Router), "BroadcastEvent", 1, 1);

        private readonly object _gate = new object();
        private readonly State _state = new State();
        private readonly IRouterDispatcher _routerDispatcher;
        private readonly ModelsEventsObservations _modelsEventsObservations;
        private readonly Dictionary<object, IModelEntry> _modelsById = new Dictionary<object, IModelEntry>();
        private readonly List<Action<Exception>> _terminalErrorHandlers = new List<Action<Exception>>();

        public Router()
            : this(new CurrentThreadDispatcher())
        {
        }

        public Router(IRouterDispatcher routerDispatcher)
        {
            Guard.Requires<ArgumentNullException>(routerDispatcher != null, "routerDispatcher can not be null");
            _routerDispatcher = routerDispatcher;
            _modelsEventsObservations = new ModelsEventsObservations();
        }

        public IEventsObservationRegistrar EventsObservationRegistrar
        {
            get
            {
                return _modelsEventsObservations;
            }
        }

        public void RegisterModel<TModel>(object modelId, TModel model)
        {
            RegisterModel(modelId, model, null, null);
        }

        public void RegisterModel<TModel>(object modelId, TModel model, IPreEventProcessor<TModel> preEventProcessor)
        {
            Guard.Requires<ArgumentNullException>(preEventProcessor != null, "preEventProcessor can not be null");
            RegisterModel(modelId, model, preEventProcessor, null);
        }

        public void RegisterModel<TModel>(object modelId, TModel model, IPostEventProcessor<TModel> postEventProcessor)
        {
            Guard.Requires<ArgumentNullException>(postEventProcessor != null, "postEventProcessor can not be null");
            RegisterModel(modelId, model, null, postEventProcessor);
        }

        public void RegisterModel<TModel>(object modelId, TModel model, IPreEventProcessor<TModel> preEventProcessor, IPostEventProcessor<TModel> postEventProcessor)
        {
            Guard.Requires<ArgumentNullException>(modelId != null, "modelId can not be null");
            Guard.Requires<ArgumentNullException>(model != null, "model can not be null");
            _state.ThrowIfHalted();
            var entry = new ModelEntry<TModel>(
                modelId,
                model,
                preEventProcessor,
                postEventProcessor,
                _state,
                _modelsEventsObservations.CreateForModel(modelId),
                new ModelChangedEventPublisher(this),
                _routerDispatcher
            );
            lock (_gate)
            {
                Guard.Requires<ArgumentException>(!_modelsById.ContainsKey(modelId), "modelId {0} already registered", modelId);
                _modelsById.Add(modelId, entry);
            }
        }

        public void RemoveModel(object modelId)
        {
            // we need to schedule the call to remove onto the dispatcher as various subjects
            // will complete as part of this operation.
            if (!_routerDispatcher.CheckAccess())
            {
                _state.ThrowIfHalted();
                _routerDispatcher.Dispatch(() => RemoveModel(modelId));
                return;
            }
            _state.ThrowIfHalted();
            IModelEntry modelEntry;
            lock (_gate)
            {
                if (!_modelsById.TryGetValue(modelId, out modelEntry)) throw new ArgumentException(string.Format("Model with id {0} not registered", modelId));
                _modelsById.Remove(modelId);
            }
            modelEntry.OnRemoved();
        }

        public void PublishEvent<TEvent>(object modelId, TEvent @event)
        {
            if (!_routerDispatcher.CheckAccess())
            {
                _state.ThrowIfHalted();
                _routerDispatcher.Dispatch(() => PublishEvent(modelId, @event));
                return;
            }
            _state.ThrowIfHalted();
            IModelEntry modelEntry = GetModelEntry(modelId);
            modelEntry.TryEnqueue(@event);
            PurgeEventQueues();
        }

        public void PublishEvent(object modelId, object @event)
        {
            var publishEventMethod = PublishEventMethodInfo.MakeGenericMethod(@event.GetType());
            try
            {
                publishEventMethod.Invoke(this, new object[] { modelId, @event });
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }

        public void ExecuteEvent<TEvent>(object modelId, TEvent @event)
        {
            _state.ThrowIfHalted();
            _routerDispatcher.EnsureAccess();
            _state.MoveToExecuting(modelId);
            IModelEntry modelEntry = GetModelEntry(modelId);
            modelEntry.ExecuteEvent(@event);
            _state.EndExecuting();
        }

        public void ExecuteEvent(object modelId, object @event)
        {
            var executeEventMethod = ExecuteEventMethodInfo.MakeGenericMethod(@event.GetType());
            try
            {
                executeEventMethod.Invoke(this, new object[] { modelId, @event });
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }

        public void BroadcastEvent<TEvent>(TEvent @event)
        {
            if (!_routerDispatcher.CheckAccess())
            {
                _state.ThrowIfHalted();
                _routerDispatcher.Dispatch(() => BroadcastEvent(@event));
                return;
            }
            _state.ThrowIfHalted();
            IModelEntry[] modelEntries;
            lock (_gate)
            {
                modelEntries = _modelsById.Values.ToArray();
            }
            foreach (IModelEntry modelEntry in modelEntries)
            {
                modelEntry.TryEnqueue(@event);
            }
            PurgeEventQueues();
        }

        public void BroadcastEvent(object @event)
        {
            var publishEventMethod = BroadcastEventMethodInfo.MakeGenericMethod(@event.GetType());
            try
            {
                publishEventMethod.Invoke(this, new object[] { @event });
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }

        public void RegisterTerminalErrorHandler(Action<Exception> onHaltingError)
        {
            lock (_gate)
            {
                _terminalErrorHandlers.Add(onHaltingError);
            }
        }

        public IModelObservable<TModel> GetModelObservable<TModel>(object modelId)
        {
            Guard.Requires<ArgumentNullException>(modelId != null, "modelId can not be null");
            _state.ThrowIfHalted();
            IModelEntry<TModel> entry = GetModelEntry<TModel>(modelId);
            return entry.GetModelObservable();
        }

        public IEventObservable<TModel, TEvent, IEventContext> GetEventObservable<TModel, TEvent>(object modelId, ObservationStage observationStage = ObservationStage.Normal)
        {
            _state.ThrowIfHalted();
            IModelEntry<TModel> entry = GetModelEntry<TModel>(modelId);
            return entry.GetEventObservable<TEvent>(observationStage);
        }

        public IEventObservable<TModel, TBaseEvent, IEventContext> GetEventObservable<TModel, TBaseEvent>(object modelId, Type subEventType, ObservationStage observationStage = ObservationStage.Normal)
        {
            _state.ThrowIfHalted();
            IModelEntry<TModel> entry = GetModelEntry<TModel>(modelId);
            return entry.GetEventObservable<TBaseEvent>(subEventType, observationStage);
        }

        public IRouter<TModel> CreateModelRouter<TModel>(object modelId)
        {
            _state.ThrowIfHalted();
            return new ModelRouter<TModel>(modelId, this);
        }

        public IRouter<TSubModel> CreateModelRouter<TModel, TSubModel>(object modelId, Func<TModel, TSubModel> subModelSelector)
        {
            _state.ThrowIfHalted();
            return new SubModelRouter<TModel, TSubModel>(modelId, this, subModelSelector);
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
                        var changedModels = new Dictionary<object, IModelEntry>();
                        while (modelEntry != null)
                        {
                            _state.MoveToPreProcessing(modelEntry.Id);
                            modelEntry.RunPreProcessor();
                            if (!modelEntry.IsRemoved)
                            {
                                _state.MoveToEventDispatch();
                                modelEntry.PurgeEventQueue();
                                if (!modelEntry.IsRemoved)
                                {
                                    _state.MoveToPostProcessing();
                                    modelEntry.RunPostProcessor();
                                }
                            }
                            modelEntry.BroadcastModelChangedEvent();
                            if (!changedModels.ContainsKey(modelEntry.Id))
                                changedModels.Add(modelEntry.Id, modelEntry);
                            modelEntry = GetNextModelEntryWithEvents();
                        }
                        _state.MoveToDispatchModelUpdates();
                        foreach (IModelEntry changedModelEntry in changedModels.Values)
                        {
                            if (!changedModelEntry.IsRemoved)
                                changedModelEntry.DispatchModel();
                        }
                        modelEntry = GetNextModelEntryWithEvents();
                    }
                    _state.MoveToIdle();
                }
                catch (Exception ex)
                {
                    _state.MoveToHalted(ex);
                    Action<Exception>[] terminalErrorHandlers;
                    lock (_gate)
                    {
                        terminalErrorHandlers = _terminalErrorHandlers.ToArray();
                    }
                    foreach (Action<Exception> terminalErrorHandler in terminalErrorHandlers)
                    {
                        terminalErrorHandler(ex); 
                    }
                    throw;
                }
            }
        }

        private IModelEntry GetNextModelEntryWithEvents()
        {
            IModelEntry modelEntry = null;
            lock (_gate)
            {
                foreach (IModelEntry entry in _modelsById.Values)
                {
                    if (entry.HadEvents)
                    {
                        modelEntry = entry;
                        break;
                    }
                }
            }
            return modelEntry;
        }

        private IModelEntry<TModel> GetModelEntry<TModel>(object modelId)
        {
            IModelEntry entry;
            lock (_gate)
            {
                if (!_modelsById.TryGetValue(modelId, out entry))
                {
                    throw new InvalidOperationException(string.Format("Model with id [{0}] isn't registered", modelId));
                }
            }
            IModelEntry<TModel> result;
            try
            {
                result = (IModelEntry<TModel>) entry;
            }
            catch (InvalidCastException)
            {
                throw new InvalidOperationException(
                    string.Format(
                        "Model with id [{0}] is not registered against model of type [{1}]. Please ensure you're using the same model type that was registered against this id.",
                        modelId, typeof (TModel).FullName));
            }
            return result;
        }

        private IModelEntry GetModelEntry(object modelId)
        {
            IModelEntry modelEntry;
            lock (_gate)
            {
                if (!_modelsById.TryGetValue(modelId, out modelEntry)) throw new ArgumentException(string.Format("Model with id {0} not registered", modelId));
            }
            return modelEntry;
        }

        // Having this type and IModelChangedEventPublisher is a bit of a 'roundabout' way of 
        // publishing model changed events. However it removes the need to use reflection to 
        // infer the closed ModelChangedEvent<TModel> type so is a bit more efficient.
        private class ModelChangedEventPublisher : IModelChangedEventPublisher
        {
            private readonly Router _parent;

            public ModelChangedEventPublisher(Router parent)
            {
                _parent = parent;
            }

            public void BroadcastEvent<TModel>(ModelChangedEvent<TModel> @event)
            {
                foreach (IModelEntry modelEntry in _parent._modelsById.Values)
                {
                    if (modelEntry.Id != @event.ModelId) modelEntry.TryEnqueue(@event);
                }
            }
        }
    }
}
