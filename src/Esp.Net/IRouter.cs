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
using Esp.Net.Reactive;

namespace Esp.Net
{
    public interface IRouter
    {
        void RegisterModel<TModel>(object modelId, TModel model);
        void RegisterModel<TModel>(object modelId, TModel model, IPreEventProcessor<TModel> preEventProcessor);
        void RegisterModel<TModel>(object modelId, TModel model, IPostEventProcessor<TModel> postEventProcessor);
        void RegisterModel<TModel>(object modelId, TModel model, IPreEventProcessor<TModel> preEventProcessor, IPostEventProcessor<TModel> postEventProcessor);
        void RemoveModel(object modelId);
        IRouter<TModel> CreateModelRouter<TModel>(object modelId);
        IRouter<TSubModel> CreateModelRouter<TModel, TSubModel>(object modelId, Func<TModel, TSubModel> subModelSelector);
    
        IModelObservable<TModel> GetModelObservable<TModel>(object modelId);
  
        IEventObservable<TModel, TEvent, IEventContext> GetEventObservable<TModel, TEvent>(object modelId, ObservationStage observationStage = ObservationStage.Normal);
        IEventObservable<TModel, TBaseEvent, IEventContext> GetEventObservable<TModel, TSubEventType, TBaseEvent>(object modelId, ObservationStage observationStage = ObservationStage.Normal) where TSubEventType : TBaseEvent;
        IEventObservable<TModel, TBaseEvent, IEventContext> GetEventObservable<TModel, TBaseEvent>(object modelId, Type subEventType, ObservationStage observationStage = ObservationStage.Normal);
  
        void PublishEvent<TEvent>(object modelId, TEvent @event);
        void PublishEvent(object modelId, object @event);

        void ExecuteEvent<TEvent>(object modelId, TEvent @event);
        void ExecuteEvent(object modelId, object @event);

        void BroadcastEvent<TEvent>(TEvent @event);
        void BroadcastEvent(object @event);
    }

    public interface IRouter<out TModel>
    {
        IModelObservable<TModel> GetModelObservable();

        IEventObservable<TModel, TEvent, IEventContext> GetEventObservable<TEvent>(ObservationStage observationStage = ObservationStage.Normal);
        IEventObservable<TModel, TBaseEvent, IEventContext> GetEventObservable<TSubEventType, TBaseEvent>(ObservationStage observationStage = ObservationStage.Normal) where TSubEventType : TBaseEvent;
        IEventObservable<TModel, TBaseEvent, IEventContext> GetEventObservable<TBaseEvent>(Type eventType, ObservationStage observationStage = ObservationStage.Normal);

        void PublishEvent<TEvent>(TEvent @event);
        void PublishEvent(object @event);

        void ExecuteEvent<TEvent>(TEvent @event);
        void ExecuteEvent(object @event);

        void BroadcastEvent<TEvent>(TEvent @event);
        void BroadcastEvent(object @event);
    }
}