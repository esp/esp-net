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
using Esp.Net.Utils;

namespace Esp.Net
{
    public class Router<TModel> : IRouter<TModel>
    {
        private object _modelIid;
        private readonly IRouter _underlying;

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
            _underlying = new Router(routerDispatcher, errorHandler);
        }

        public Router(object modelId, IRouter underlying)
        {
            _modelIid = modelId;
            _underlying = underlying;
        }

        // exists to avoid the chicken and egg problem whereby the Router needs the model and the model needs the router
        public void SetModel(object modelId, TModel model)
        {
            if (_modelIid == null)
            {
                _modelIid = modelId;
                AddModelInternal(model);
            }
        }

        // exists to avoid the chicken and egg problem whereby the Router needs the model and the model needs the router
        public void SetModel(TModel model)
        {
            if (_modelIid == null)
            {
                _modelIid = Guid.NewGuid();
                AddModelInternal(model);
            }
        }

        public IModelObservable<TModel> GetModelObservable()
        {
            EnsureModel();
            return _underlying.GetModelObservable<TModel>(_modelIid);
        }

        public IEventObservable<TModel, TEvent, IEventContext> GetEventObservable<TEvent>(ObservationStage observationStage = ObservationStage.Normal)
        {
            EnsureModel();
            return _underlying.GetEventObservable<TModel, TEvent>(_modelIid, observationStage);
        }

        public IEventObservable<TModel, TBaseEvent, IEventContext> GetEventObservable<TBaseEvent>(Type eventType, ObservationStage observationStage = ObservationStage.Normal)
        {
            EnsureModel();
            return _underlying.GetEventObservable<TModel, TBaseEvent>(_modelIid, eventType, observationStage);
        }

        public void PublishEvent<TEvent>(TEvent @event)
        {
            EnsureModel();
            _underlying.PublishEvent(_modelIid, @event);
        }

        public void PublishEvent(object @event)
        {
            EnsureModel();
            _underlying.PublishEvent(_modelIid, @event);
        }

        public void ExecuteEvent<TEvent>(TEvent @event)
        {
            EnsureModel();
            _underlying.ExecuteEvent(_modelIid, @event);
        }

        public void ExecuteEvent(object @event)
        {
            EnsureModel();
            _underlying.ExecuteEvent(_modelIid, @event);
        }

        public void RunAction(Action<TModel> action)
        {
            EnsureModel();
            _underlying.RunAction(_modelIid, action);
        }

        public void RunAction(Action action)
        {
            EnsureModel();
            _underlying.RunAction(_modelIid, action);
        }

        private void AddModelInternal(TModel model)
        {
            EnsureModel();
            _underlying.AddModel(_modelIid, model);
        }

        private void EnsureModel()
        {
            Guard.Requires<InvalidOperationException>(_modelIid != null, "Model not set");
        }
    }
}