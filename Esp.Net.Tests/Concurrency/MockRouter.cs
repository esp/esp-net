using System;
using System.Collections.Generic;
using Esp.Net.Reactive;
using Moq;

namespace Esp.Net.Concurrency
{
    public class MockRouter<TModel> : Mock<IRouter<TModel>>
    {
        private readonly TModel _model;
        private readonly Dictionary<Type, object> _eventSubjects;

        public MockRouter(TModel model)
        {
            _eventSubjects = new Dictionary<Type, object>();
            _model = model;
        }

        internal EventSubject<TModel, TEvent, IEventContext> GetEventSubject<TEvent>()
        {
            return GetOrSetEventSubject<TEvent>();
        }

        public MockRouter<TModel> SetUpEventStream<TEvent>()
        {
            var subject = GetOrSetEventSubject<TEvent>();
            Setup(r => r.GetEventObservable<TEvent>(ObservationStage.Normal))
                .Returns(subject);
            Setup(r => r.PublishEvent(It.IsAny<TEvent>())).Callback((TEvent e) =>
            {
                PublishEvent(e);
            });
            return this;
        }

        public void PublishEvent<TEvent>(TEvent e)
        {
            dynamic subject = _eventSubjects[typeof (TEvent)];
            subject.OnNext(_model, e, new EventContext());
        }

        private EventSubject<TModel, TEvent, IEventContext> GetOrSetEventSubject<TEvent>()
        {
            EventSubject<TModel, TEvent, IEventContext> result;
            object subject;
            if (!_eventSubjects.TryGetValue(typeof (TEvent), out subject))
            {
                result = new EventSubject<TModel, TEvent, IEventContext>();
                _eventSubjects.Add(typeof(TEvent), result);
            }
            else
            {
                result = (EventSubject<TModel, TEvent, IEventContext>) subject;
            }
            return result;
        }
    }
}