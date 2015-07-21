using System;
using System.Collections.Generic;
using System.Security.Policy;
using Esp.Net.Model;
using Esp.Net.Reactive;
using Esp.Net.Router;

namespace Esp.Net.Stubs
{
    public class StubRouter : IRouter
    {
        public StubRouter()
        {
            ModelEnteries = new Dictionary<Guid, Dictionary<Type, dynamic>>();
        }

        public Dictionary<Guid, Dictionary<Type, dynamic>> ModelEnteries { get; private set; }

        public void PublishEvent<TEvent>(Guid modelId, TEvent @event)
        {
            dynamic subject = ModelEnteries[modelId][typeof (TEvent)];
            subject.OnNext(@event);
        }

        internal StubEventSubject<TModel, TEvent, IEventContext> GetEventSubject<TModel, TEvent>(Guid modelId)
        {
            return GetOrSetEventSubject<TModel, TEvent>(modelId);
        }

        public void RegisterModel<TModel>(Guid modelId, TModel model)
        {
        }

        public void RegisterModel<TModel>(Guid modelId, TModel model, IPreEventProcessor<TModel> preEventProcessor)
        {
        }

        public void RegisterModel<TModel>(Guid modelId, TModel model, IPostEventProcessor<TModel> postEventProcessor)
        {
        }

        public void RegisterModel<TModel>(Guid modelId, TModel model, IPreEventProcessor<TModel> preEventProcessor,
            IPostEventProcessor<TModel> postEventProcessor)
        {
        }

        public IModelObservable<TModel> GetModelObservable<TModel>(Guid modelId)
        {
            throw new NotImplementedException();
        }

        public IEventObservable<TModel, TEvent, IEventContext> GetEventObservable<TModel, TEvent>(Guid modelId, ObservationStage observationStage = ObservationStage.Normal)
        {
            var subject = GetOrSetEventSubject<TModel, TEvent>(modelId);
            return subject;
        }

        public IEventObservable<TModel, TBaseEvent, IEventContext> GetEventObservable<TModel, TSubEventType, TBaseEvent>(
            Guid modelId, 
            ObservationStage observationStage = ObservationStage.Normal) where TSubEventType : TBaseEvent
        {
            throw new NotImplementedException();
        }

        public IEventObservable<TModel, TBaseEvent, IEventContext> GetEventObservable<TModel, TBaseEvent>(Guid modelId, Type eventType,
            ObservationStage observationStage = ObservationStage.Normal)
        {
            throw new NotImplementedException();
        }

        public IModelRouter<TModel> CreateModelRouter<TModel>(Guid modelId)
        {
            throw new NotImplementedException();
        }

        private StubEventSubject<TModel, TEvent, IEventContext> GetOrSetEventSubject<TModel, TEvent>(Guid modelId)
        {
            Dictionary<Type, dynamic> modelEntry;
            if (!ModelEnteries.TryGetValue(modelId, out modelEntry))
            {
                modelEntry = new Dictionary<Type, dynamic>();
                ModelEnteries.Add(modelId, modelEntry);
            }

            // it's eaiser to just use a real subject here rather than mocking that.
            StubEventSubject<TModel, TEvent, IEventContext> result;
            object subject;
            if (!modelEntry.TryGetValue(typeof(TEvent), out subject))
            {
                result = new StubEventSubject<TModel, TEvent, IEventContext>();
                modelEntry.Add(typeof(TEvent), result);
            }
            else
            {
                result = (StubEventSubject<TModel, TEvent, IEventContext>)subject;
            }
            return result;
        }
    }
}