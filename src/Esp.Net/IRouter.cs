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
    public interface IRouter
    {
        void AddModel<TModel>(object modelId, TModel model);
        void AddModel<TModel>(object modelId, TModel model, IPreEventProcessor<TModel> preEventProcessor);
        void AddModel<TModel>(object modelId, TModel model, IPostEventProcessor<TModel> postEventProcessor);
        void AddModel<TModel>(object modelId, TModel model, IPreEventProcessor<TModel> preEventProcessor, IPostEventProcessor<TModel> postEventProcessor);
        void RemoveModel(object modelId);
        IRouter<TModel> CreateModelRouter<TModel>(object modelId);

        IModelObservable<TModel> GetModelObservable<TModel>(object modelId);
  
        IEventObservable<TEvent, IEventContext, TModel> GetEventObservable<TEvent, TModel>(object modelId, ObservationStage observationStage = ObservationStage.Normal);
        // IEventObservable<TBaseEvent, IEventContext, TModel> GetEventObservable<TBaseEvent, TModel>(object modelId, Type subEventType, ObservationStage observationStage = ObservationStage.Normal);
  
        void PublishEvent<TEvent>(object modelId, TEvent @event);
        void PublishEvent(object modelId, object @event);

        void ExecuteEvent<TEvent>(object modelId, TEvent @event);
        void ExecuteEvent(object modelId, object @event);

        void BroadcastEvent<TEvent>(TEvent @event);
        void BroadcastEvent(object @event);

        void RunAction<TModel>(object modelId, Action<TModel> action);
        void RunAction(object modelId, Action action);
    }
}