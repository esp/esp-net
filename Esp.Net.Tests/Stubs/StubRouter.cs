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
            RouterEntries = new Dictionary<Guid, RouterEntry>();
        }

        public class RouterEntry
        {
            public Guid Id { get; set; }
            public object Model { get; set; }
            public Dictionary<Type, object> EventSubjects { get; private set; }

            private StubEventSubject<TModel, TEvent, IEventContext> GetOrSetEventSubject<TEvent>(Guid modelId)
            {
                // it's eaiser to just use a real subject here rather than mocking that.
                StubEventSubject<TModel, TEvent, IEventContext> result;
                object subject;
                if (!EventSubjects.TryGetValue(typeof(TEvent), out subject))
                {
                    result = new StubEventSubject<TModel, TEvent, IEventContext>();
                    EventSubjects.Add(typeof(TEvent), result);
                }
                else
                {
                    result = (StubEventSubject<TModel, TEvent, IEventContext>)subject;
                }
                return result;
            }
        }

        public Dictionary<Guid, RouterEntry> RouterEntries { get; private set; }

        public void PublishEvent<TEvent>(Guid modelId, TEvent @event)
        {
            var subject = GetOrSetEventSubject<TModel, TEvent>(modelId);
            subject.OnNext(_model, @event, new EventContext());
        }

        internal StubEventSubject<TModel, TEvent, IEventContext> GetEventSubject<TModel, TEvent>()
        {
            return GetOrSetEventSubject<TModel, TEvent>();
        }

        public IModelObservable<TModel> GetModelObservable<TModel>(Guid modelId)
        {
            throw new NotImplementedException();
        }

        public IEventObservable<TModel, TEvent, IEventContext> GetEventObservable<TModel, TEvent>(Guid modelId, ObservationStage observationStage = ObservationStage.Normal)
        {
            var subject = GetOrSetEventSubject<TModel, TEvent>();
            return subject;
        }

        public IEventObservable<TModel, TBaseEvent, IEventContext> GetEventObservable<TModel, TSubEventType, TBaseEvent>(
            Guid modelId, 
            ObservationStage observationStage = ObservationStage.Normal) where TSubEventType : TBaseEvent
        {
            throw new NotImplementedException();
        }

        public IEventObservable<TModel, TBaseEvent, IEventContext> GetEventObservable<TModel, TBaseEvent>(Type eventType,
            ObservationStage observationStage = ObservationStage.Normal)
        {
            throw new NotImplementedException();
        }


    }
}