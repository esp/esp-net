using System;
using System.Collections.Generic;
using Esp.Net.Reactive;

namespace Esp.Net.Stubs
{
    public class StubRouter<TModel> : IRouter<TModel>
    {
        private readonly TModel _model;

        public StubRouter(TModel model)
        {
            EventSubjects = new Dictionary<Type, object>();
            _model = model;
        }

        public Dictionary<Type, object> EventSubjects { get; private set; }

        public void PublishEvent<TEvent>(TEvent @event)
        {
            var subject = GetOrSetEventSubject<TEvent>();
            subject.OnNext(_model, @event, new EventContext());
        }

        internal StubEventSubject<TModel, TEvent, IEventContext> GetEventSubject<TEvent>()
        {
            return GetOrSetEventSubject<TEvent>();
        }

        public IModelObservable<TModel> GetModelObservable()
        {
            throw new NotImplementedException();
        }

        public IEventObservable<TModel, TEvent, IEventContext> GetEventObservable<TEvent>(ObservationStage observationStage = ObservationStage.Normal)
        {
            var subject = GetOrSetEventSubject<TEvent>();
            return subject;
        }

        public IEventObservable<TModel, TBaseEvent, IEventContext> GetEventObservable<TBaseEvent>(Type eventType,
            ObservationStage observationStage = ObservationStage.Normal)
        {
            throw new NotImplementedException();
        }

        private StubEventSubject<TModel, TEvent, IEventContext> GetOrSetEventSubject<TEvent>()
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
}