using System;
using System.Collections.Generic;
using Esp.Net.Model;
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

        internal TestEventSubject<TModel, TEvent, IEventContext> GetEventSubject<TEvent>()
        {
            return GetOrSetEventSubject<TEvent>();
        }

        public MockRouter<TModel> SetUpEventStream<TEvent>()
        {
            var subject = GetOrSetEventSubject<TEvent>();
            Setup(r => r.GetEventObservable<TEvent>(ObservationStage.Normal))
                .Returns(subject.Object);
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

        private TestEventSubject<TModel, TEvent, IEventContext> GetOrSetEventSubject<TEvent>()
        {
            // it's eaiser to just use a real subject here rather than mocking that.
            TestEventSubject<TModel, TEvent, IEventContext> result;
            object subject;
            if (!_eventSubjects.TryGetValue(typeof (TEvent), out subject))
            {
                result = new TestEventSubject<TModel, TEvent, IEventContext>();
                _eventSubjects.Add(typeof(TEvent), result);
            }
            else
            {
                result = (TestEventSubject<TModel, TEvent, IEventContext>)subject;
            }
            return result;
        }
    }

    public class TestEventSubject<TModel, TEvent, TContext> : Mock<ITestEventSubject<TModel, TEvent, TContext>>
    {
        public List<Action<TModel, TEvent, TContext>> Observers { get; private set; }

        public int DisposedCount { get; set; }

        public TestEventSubject()
        {
            var observerDisposable = EspDisposable.Create(() => DisposedCount++);
            Observers = new List<Action<TModel, TEvent, TContext>>();
            Setup(s => s.Observe(It.IsAny<Action<TModel, TEvent, TContext>>())).Callback(
                (Action<TModel, TEvent, TContext> o) =>
                {
                    Observers.Add(o);
                }).Returns(observerDisposable);
            Setup(s => s.Observe(It.IsAny<Action<TModel, TEvent>>())).Callback(
                (Action<TModel, TEvent> o) =>
                {
                    Action<TModel, TEvent, TContext> a = (m, e,c) => o(m, e);
                    Observers.Add(a);
                }).Returns(observerDisposable);
            Setup(s => s.Observe(It.IsAny<IEventObserver<TModel, TEvent, TContext>>())).Callback(
                (IEventObserver<TModel, TEvent, TContext> o) =>
                {
                    Action<TModel, TEvent, TContext> a = o.OnNext;
                    Observers.Add(a);
                }).Returns(observerDisposable);
        }

        public void OnNext(TModel model, TEvent e, TContext context)
        {
            foreach (Action<TModel, TEvent, TContext> eventObserver in Observers)
            {
                eventObserver(model, e, context);
            }
        }
    }

    public interface ITestEventSubject<TModel, TEvent, TContext> : IEventObservable<TModel, TEvent, TContext>, IEventObserver<TModel, TEvent, TContext>
    {
    }
}