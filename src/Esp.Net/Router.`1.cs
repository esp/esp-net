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

namespace Esp.Net
{
    public class Router<TModel> : IRouter<TModel>
    {
        private object _modelIid;
        private readonly IRouter _underlying;

        public Router(TModel model)
            : this(model, new CurrentThreadDispatcher(), null)
        {
        }

        public Router(TModel model, IRouterDispatcher routerDispatcher)
            :this(model, routerDispatcher, null)
        {

        }

        public Router(TModel model, ITerminalErrorHandler errorHandler)
            : this(model, new CurrentThreadDispatcher(), errorHandler)
        {

        }

        public Router(TModel model, IRouterDispatcher routerDispatcher, ITerminalErrorHandler errorHandler)
        {
            _underlying = new Router(routerDispatcher, errorHandler);
            AddModelInternal(model);
        }

        public Router(TModel model, IRouter underlying)
        {
            _underlying = underlying;
            AddModelInternal(model);
        }

        public Router(object modelId, IRouter underlying)
        {
            _underlying = underlying;
            _modelIid = modelId;
        }

        public IModelObservable<TModel> GetModelObservable()
        {
            return _underlying.GetModelObservable<TModel>(_modelIid);
        }

        public IEventObservable<TModel, TEvent, IEventContext> GetEventObservable<TEvent>(ObservationStage observationStage = ObservationStage.Normal)
        {
            return _underlying.GetEventObservable<TModel, TEvent>(_modelIid, observationStage);
        }

        public IEventObservable<TModel, TBaseEvent, IEventContext> GetEventObservable<TBaseEvent>(Type eventType, ObservationStage observationStage = ObservationStage.Normal)
        {
            return _underlying.GetEventObservable<TModel, TBaseEvent>(_modelIid, eventType, observationStage);
        }

        public void PublishEvent<TEvent>(TEvent @event)
        {
            _underlying.PublishEvent(_modelIid, @event);
        }

        public void PublishEvent(object @event)
        {
            _underlying.PublishEvent(_modelIid, @event);
        }

        public void ExecuteEvent<TEvent>(TEvent @event)
        {
            _underlying.ExecuteEvent(_modelIid, @event);
        }

        public void ExecuteEvent(object @event)
        {
            _underlying.ExecuteEvent(_modelIid, @event);
        }

        public void RunAction(Action<TModel> action)
        {
            _underlying.RunAction(_modelIid, action);
        }

        public void RunAction(Action action)
        {
            _underlying.RunAction(_modelIid, action);
        }

        private void AddModelInternal(TModel model)
        {
            _modelIid = Guid.NewGuid();
            _underlying.AddModel(_modelIid, model);
        }
    }
}