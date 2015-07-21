using System;
using Esp.Net.Model;
using Esp.Net.Reactive;

namespace Esp.Net.Router
{
    public interface IRouter : IEventPublisher
    {
        void RegisterModel<TModel>(Guid modelId, TModel model);
        void RegisterModel<TModel>(Guid modelId, TModel model, IPreEventProcessor<TModel> preEventProcessor);
        void RegisterModel<TModel>(Guid modelId, TModel model, IPostEventProcessor<TModel> postEventProcessor);
        void RegisterModel<TModel>(Guid modelId, TModel model, IPreEventProcessor<TModel> preEventProcessor, IPostEventProcessor<TModel> postEventProcessor);
        IModelObservable<TModel> GetModelObservable<TModel>(Guid modelId);
        IEventObservable<TModel, TEvent, IEventContext> GetEventObservable<TModel, TEvent>(Guid modelId, ObservationStage observationStage = ObservationStage.Normal);
        IEventObservable<TModel, TBaseEvent, IEventContext> GetEventObservable<TModel, TSubEventType, TBaseEvent>(Guid modelId, ObservationStage observationStage = ObservationStage.Normal) where TSubEventType : TBaseEvent;
        IEventObservable<TModel, TBaseEvent, IEventContext> GetEventObservable<TModel, TBaseEvent>(Guid modelId, Type eventType, ObservationStage observationStage = ObservationStage.Normal);
        IModelRouter<TModel> CreateModelRouter<TModel>(Guid modelId);
    }
}