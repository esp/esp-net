using System;
using Esp.Net.Reactive;

namespace Esp.Net
{
    public interface IThreadGuard
    {
        bool CheckAccess();
    }

    public interface IRouter : IModelSubject, IEventSubject, IEventPublisher
    {
        void RegisterModel<TModel>(Guid modelId, TModel model);
        void RegisterModel<TModel>(Guid modelId, TModel model, IPreEventProcessor<TModel> preEventProcessor);
        void RegisterModel<TModel>(Guid modelId, TModel model, IPostEventProcessor<TModel> postEventProcessor);
        void RegisterModel<TModel>(Guid modelId, TModel model, IPreEventProcessor<TModel> preEventProcessor, IPostEventProcessor<TModel> postEventProcessor);
        IRouter<TModel> CreateModelRouter<TModel>(Guid modelId);
    }

    public interface IRouter<out TModel> : IModelEventPublisher
    {
        IEventObservable<TModel, TEvent, IEventContext> GetEventObservable<TEvent>(ObservationStage observationStage = ObservationStage.Normal);
        IEventObservable<TModel, TBaseEvent, IEventContext> GetEventObservable<TSubEventType, TBaseEvent>(ObservationStage observationStage = ObservationStage.Normal) where TSubEventType : TBaseEvent;
        IEventObservable<TModel, TBaseEvent, IEventContext> GetEventObservable<TBaseEvent>(Type eventType, ObservationStage observationStage = ObservationStage.Normal);
    }

    public interface IModelSubject
    {
        IModelObservable<TModel> GetModelObservable<TModel>(Guid modelId);
    }
    
    public interface IModelSubject<out TModel>
    {
        IModelObservable<TModel> GetModelObservable();
    }

    public interface IEventSubject
    {
        IEventObservable<TModel, TEvent, IEventContext> GetEventObservable<TModel, TEvent>(Guid modelId, ObservationStage observationStage = ObservationStage.Normal);
        IEventObservable<TModel, TBaseEvent, IEventContext> GetEventObservable<TModel, TSubEventType, TBaseEvent>(Guid modelId, ObservationStage observationStage = ObservationStage.Normal) where TSubEventType : TBaseEvent;
        IEventObservable<TModel, TBaseEvent, IEventContext> GetEventObservable<TModel, TBaseEvent>(Guid modelId, Type eventType, ObservationStage observationStage = ObservationStage.Normal);
    }

    public interface IEventSubject<out TModel>
    {
        IEventObservable<TModel, TEvent, IEventContext> GetEventObservable<TEvent>(ObservationStage observationStage = ObservationStage.Normal);
        IEventObservable<TModel, TBaseEvent, IEventContext> GetEventObservable<TSubEventType, TBaseEvent>(ObservationStage observationStage = ObservationStage.Normal) where TSubEventType : TBaseEvent;
        IEventObservable<TModel, TBaseEvent, IEventContext> GetEventObservable<TBaseEvent>(Type eventType, ObservationStage observationStage = ObservationStage.Normal);
    }

    public interface IEventPublisher
    {
        void PublishEvent<TEvent>(Guid modelId, TEvent @event);
    }

    public interface IModelEventPublisher
    {
        void PublishEvent<TEvent>(TEvent @event);
    }
}