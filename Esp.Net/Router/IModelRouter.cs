using System;
using Esp.Net.Model;
using Esp.Net.Reactive;

namespace Esp.Net.Router
{
    public interface IModelRouter<out TModel> : IModelEventPublisher
    {
        IModelObservable<TModel> GetModelObservable();
        IEventObservable<TModel, TEvent, IEventContext> GetEventObservable<TEvent>(ObservationStage observationStage = ObservationStage.Normal);
        IEventObservable<TModel, TBaseEvent, IEventContext> GetEventObservable<TSubEventType, TBaseEvent>(ObservationStage observationStage = ObservationStage.Normal) where TSubEventType : TBaseEvent;
        IEventObservable<TModel, TBaseEvent, IEventContext> GetEventObservable<TBaseEvent>(Type eventType, ObservationStage observationStage = ObservationStage.Normal);
    }

    public class ModelRouter<TModel> : IModelRouter<TModel>
    {
        private readonly Guid _modelIid;
        private readonly IRouter _underlying;

        public ModelRouter(Guid modelIid, IRouter underlying)
        {
            _modelIid = modelIid;
            _underlying = underlying;
        }

        public void PublishEvent<TEvent>(TEvent @event)
        {
            _underlying.PublishEvent(_modelIid, @event);
        }

        public IModelObservable<TModel> GetModelObservable()
        {
            return _underlying.GetModelObservable<TModel>(_modelIid);
        }

        public IEventObservable<TModel, TEvent, IEventContext> GetEventObservable<TEvent>(ObservationStage observationStage = ObservationStage.Normal)
        {
            return _underlying.GetEventObservable<TModel, TEvent>(_modelIid, observationStage);
        }

        public IEventObservable<TModel, TBaseEvent, IEventContext> GetEventObservable<TSubEventType, TBaseEvent>(ObservationStage observationStage = ObservationStage.Normal) where TSubEventType : TBaseEvent
        {
            return _underlying.GetEventObservable<TModel, TSubEventType, TBaseEvent>(_modelIid, observationStage);
        }

        public IEventObservable<TModel, TBaseEvent, IEventContext> GetEventObservable<TBaseEvent>(Type eventType, ObservationStage observationStage = ObservationStage.Normal)
        {
            return _underlying.GetEventObservable<TModel, TBaseEvent>(_modelIid, eventType, observationStage);
        }
    }
}