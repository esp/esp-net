#region copyright
// Copyright 2015 Dev Shop Limited
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
            : this(new CurrentThreadDispatcher())
        {
        }

        public Router(IRouterDispatcher routerDispatcher)
            : this(routerDispatcher, null)
        {
        }

        public Router(IRouterDispatcher routerDispatcher, ITerminalErrorHandler errorHandler)
        {
            Guard.Requires<ArgumentNullException>(routerDispatcher != null, "routerDispatcher can not be null");
            _underlying = new Router(routerDispatcher, errorHandler);
        }

        public Router(TModel model)
            : this(model, new CurrentThreadDispatcher())
        {
        }

        public Router(TModel model, IRouterDispatcher routerDispatcher)
            : this(model, routerDispatcher, null)
        {
        }

        public Router(TModel model, IRouterDispatcher routerDispatcher, ITerminalErrorHandler errorHandler)
        {
            Guard.Requires<ArgumentNullException>(model != null, "model can not be null");
            Guard.Requires<ArgumentNullException>(routerDispatcher != null, "routerDispatcher can not be null");
            _underlying = new Router(routerDispatcher, errorHandler);
            AddModelInternal(model);
        }

        public Router(object modelId, IRouter underlying)
        {
            Guard.Requires<ArgumentNullException>(modelId != null, "modelId can not be null");
            Guard.Requires<ArgumentNullException>(underlying != null, "underlying IRouter can not be null");
            _underlying = underlying;
            _modelIid = modelId;
        }

        public void SetModel(TModel model)
        {
            AddModelInternal(model);
        }

        public IModelObservable<TModel> GetModelObservable()
        {
            if (_modelIid == null) ThrowAsModelNotSet();
            return _underlying.GetModelObservable<TModel>(_modelIid);
        }

        public IEventObservable<TEvent, IEventContext, TModel> GetEventObservable<TEvent>(ObservationStage observationStage = ObservationStage.Normal)
        {
            if (_modelIid == null) ThrowAsModelNotSet();
            return _underlying.GetEventObservable<TEvent, TModel>(_modelIid, observationStage);
        }

        public void PublishEvent<TEvent>(TEvent @event)
        {
            if (_modelIid == null) ThrowAsModelNotSet();
            _underlying.PublishEvent(_modelIid, @event);
        }

        public void PublishEvent(object @event)
        {
            if (_modelIid == null) ThrowAsModelNotSet();
            _underlying.PublishEvent(_modelIid, @event);
        }

        public void ExecuteEvent<TEvent>(TEvent @event)
        {
            if (_modelIid == null) ThrowAsModelNotSet();
            _underlying.ExecuteEvent(_modelIid, @event);
        }

        public void ExecuteEvent(object @event)
        {
            if (_modelIid == null) ThrowAsModelNotSet();
            _underlying.ExecuteEvent(_modelIid, @event);
        }

        public void RunAction(Action<TModel> action)
        {
            if (_modelIid == null) ThrowAsModelNotSet();
            _underlying.RunAction(_modelIid, action);
        }

        public void RunAction(Action action)
        {
            if (_modelIid == null) ThrowAsModelNotSet();
            _underlying.RunAction(_modelIid, action);
        }

        private void AddModelInternal(TModel model)
        {
            Guard.Requires<InvalidOperationException>(_modelIid == null, "Model already set");
            _modelIid = Guid.NewGuid();
            _underlying.AddModel(_modelIid, model);
        }

        private void ThrowAsModelNotSet()
        {
            throw new InvalidOperationException("Model not set. You must call router.SetModel(model) passing the model.");
        }
    }
}
