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
        private readonly ITerminalErrorHandler _errorHandler;
        private readonly ModelsEventsObservations _modelsEventsObservations;
        private readonly Dictionary<object, IModelRouter> _modelsById = new Dictionary<object, IModelRouter>();

        public Router()
            : this(new CurrentThreadDispatcher(), null)
        {
        }

        public Router(IRouterDispatcher routerDispatcher)
            : this(routerDispatcher, null)
        {
        }

        public Router(ITerminalErrorHandler errorHandler)
            : this(new CurrentThreadDispatcher(), errorHandler)
        {
        }

        public Router(IRouterDispatcher routerDispatcher, ITerminalErrorHandler errorHandler)
        {
            Guard.Requires<ArgumentNullException>(routerDispatcher != null, "routerDispatcher can not be null");
            _routerDispatcher = routerDispatcher;
            _errorHandler = errorHandler;
            _modelsEventsObservations = new ModelsEventsObservations();
        }

        public IEventsObservationRegistrar EventsObservationRegistrar
        {
            get
            {
                return _modelsEventsObservations;
            }
        }

        public void AddModel<TModel>(object modelId, TModel model)
        {
            AddModel(modelId, model, null, null);
        }

        public void AddModel<TModel>(object modelId, TModel model, IPreEventProcessor<TModel> preEventProcessor)
        {
            Guard.Requires<ArgumentNullException>(preEventProcessor != null, "preEventProcessor can not be null");
            AddModel(modelId, model, preEventProcessor, null);
        }

        public void AddModel<TModel>(object modelId, TModel model, IPostEventProcessor<TModel> postEventProcessor)
        {
            Guard.Requires<ArgumentNullException>(postEventProcessor != null, "postEventProcessor can not be null");
            AddModel(modelId, model, null, postEventProcessor);
        }

        public void AddModel<TModel>(object modelId, TModel model, IPreEventProcessor<TModel> preEventProcessor, IPostEventProcessor<TModel> postEventProcessor)
        {
            Guard.Requires<ArgumentNullException>(modelId != null, "modelId can not be null");
            Guard.Requires<ArgumentNullException>(model != null, "model can not be null");
            _state.ThrowIfHalted();
            var entry = new ModelRouter<TModel>(
                modelId,
                model,
                preEventProcessor,
                postEventProcessor,
                _state,
                _modelsEventsObservations.CreateForModel(modelId),
                new ModelChangedEventPublisher(this)
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
            IModelRouter modelRouter;
            lock (_gate)
            {
                if (!_modelsById.TryGetValue(modelId, out modelRouter)) throw new ArgumentException(string.Format("Model with id {0} not registered", modelId));
                _modelsById.Remove(modelId);
            }
            modelRouter.OnRemoved();
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
            IModelRouter modelRouter = GetModelRouter(modelId);
            modelRouter.TryEnqueue(@event);
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
            IModelRouter modelRouter = GetModelRouter(modelId);
            modelRouter.ExecuteEvent(@event);
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
            IModelRouter[] modelRouters;
            lock (_gate)
            {
                modelRouters = _modelsById.Values.ToArray();
            }
            foreach (IModelRouter modelEntry in modelRouters)
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

        public void RunAction<TModel>(object modelId, Action<TModel> action)
        {
            if (!_routerDispatcher.CheckAccess())
            {
                _state.ThrowIfHalted();
                _routerDispatcher.Dispatch(() => RunAction(modelId, action));
                return;
            }
            _state.ThrowIfHalted();
            IModelRouter modelRouter = GetModelRouter(modelId);
            modelRouter.RunAction(action);
            PurgeEventQueues();
        }

        public void RunAction(object modelId, Action action)
        {
            if (!_routerDispatcher.CheckAccess())
            {
                _state.ThrowIfHalted();
                _routerDispatcher.Dispatch(() => RunAction(modelId, action));
                return;
            }
            _state.ThrowIfHalted();
            IModelRouter modelRouter = GetModelRouter(modelId);
            modelRouter.RunAction(action);
            PurgeEventQueues();
        }

        public IModelObservable<TModel> GetModelObservable<TModel>(object modelId)
        {
            Guard.Requires<ArgumentNullException>(modelId != null, "modelId can not be null");
            _state.ThrowIfHalted();
            IModelRouter<TModel> modelRouter = GetModelRouter<TModel>(modelId);
            return modelRouter.GetModelObservable();
        }

        public IEventObservable<TEvent, IEventContext, TModel> GetEventObservable<TEvent, TModel>(object modelId, ObservationStage observationStage = ObservationStage.Normal)
        {
            _state.ThrowIfHalted();
            IModelRouter<TModel> modelRouter = GetModelRouter<TModel>(modelId);
            return modelRouter.GetEventObservable<TEvent>(observationStage);
        }

        public IEventObservable<TBaseEvent, IEventContext, TModel> GetEventObservable<TBaseEvent, TModel>(object modelId, Type subEventType, ObservationStage observationStage = ObservationStage.Normal)
        {
            _state.ThrowIfHalted();
            IModelRouter<TModel> modelRouter = GetModelRouter<TModel>(modelId);
            return modelRouter.GetEventObservable<TBaseEvent>(subEventType, observationStage);
        }

        public IRouter<TModel> CreateModelRouter<TModel>(object modelId)
        {
            return new Router<TModel>(modelId, this);
        } 

        private void PurgeEventQueues()
        {
            if (_state.CurrentStatus == Status.Idle)
            {
                try
                {
                    IModelRouter modelRouter = GetNextModelEntryWithEvents();
                    while (modelRouter != null)
                    {
                        var changedModels = new Dictionary<object, IModelRouter>();
                        while (modelRouter != null)
                        {
                            _state.MoveToPreProcessing(modelRouter.Id);
                            modelRouter.RunPreProcessor();
                            if (!modelRouter.IsRemoved)
                            {
                                _state.MoveToEventDispatch();
                                modelRouter.PurgeEventQueue();
                                if (!modelRouter.IsRemoved)
                                {
                                    _state.MoveToPostProcessing();
                                    modelRouter.RunPostProcessor();
                                }
                            }
                            modelRouter.BroadcastModelChangedEvent();
                            if (!changedModels.ContainsKey(modelRouter.Id))
                                changedModels.Add(modelRouter.Id, modelRouter);
                            modelRouter = GetNextModelEntryWithEvents();
                        }
                        _state.MoveToDispatchModelUpdates();
                        foreach (IModelRouter changedModelEntry in changedModels.Values)
                        {
                            if (!changedModelEntry.IsRemoved)
                                changedModelEntry.DispatchModel();
                        }
                        modelRouter = GetNextModelEntryWithEvents();
                    }
                    _state.MoveToIdle();
                }
                catch (Exception ex)
                {
                    _state.MoveToHalted(ex);
                    if (_errorHandler != null)
                        _errorHandler.OnError(ex);
                    else
                        throw;
                }
            }
        }

        private IModelRouter GetNextModelEntryWithEvents()
        {
            IModelRouter modelRouter = null;
            lock (_gate)
            {
                foreach (IModelRouter entry in _modelsById.Values)
                {
                    if (entry.HadEvents)
                    {
                        modelRouter = entry;
                        break;
                    }
                }
            }
            return modelRouter;
        }

        private IModelRouter<TModel> GetModelRouter<TModel>(object modelId)
        {
            IModelRouter router;
            lock (_gate)
            {
                if (!_modelsById.TryGetValue(modelId, out router))
                {
                    throw new InvalidOperationException(string.Format("Model with id [{0}] isn't registered", modelId));
                }
            }
            IModelRouter<TModel> result;
            try
            {
                result = (IModelRouter<TModel>) router;
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

        private IModelRouter GetModelRouter(object modelId)
        {
            IModelRouter modelRouter;
            lock (_gate)
            {
                if (!_modelsById.TryGetValue(modelId, out modelRouter)) throw new ArgumentException(string.Format("Model with id {0} not registered", modelId));
            }
            return modelRouter;
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
                foreach (IModelRouter modelEntry in _parent._modelsById.Values)
                {
                    if (modelEntry.Id != @event.ModelId) modelEntry.TryEnqueue(@event);
                }
            }
        }
    }
}
