using System;
using System.Collections.Generic;
using Esp.Net.Model;
using Esp.Net.Reactive;
using NUnit.Framework;
using Shouldly;

#if ESP_EXPERIMENTAL
namespace Esp.Net.HeldEvents
{
    [TestFixture]
    public class HeldEventTests
    {
        private TestModel _model;
        private Router<TestModel> _router;
        private List<FooEvent> _receivedFooEvents;
        private IDisposable _fooEventStreamDisposable;
        private List<BarEvent> _receivedBarEvents;
        private IDisposable _barEventStreamDisposable;

        public class TestModel : IHeldEventStore
        {
            public TestModel()
            {
                HeldEvents = new List<IEventDescription>();
            }

            public bool HoldAllEvents { get; set; }

            public IList<IEventDescription> HeldEvents { get; private set; }

            public void AddHeldEventDescription(IEventDescription e)
            {
                HeldEvents.Add(e);
            }

            public void RemoveHeldEventDescription(IEventDescription e)
            {
                HeldEvents.Remove(e);
            }
        }

        public class BaseEvent : IIdentifiableEvent 
        {
            public BaseEvent()
            {
                Id = Guid.NewGuid();
            }

            public Guid Id { get; private set; }
        }

        public class FooEvent : BaseEvent
        {
            public FooEvent(string payload)
            {
                Payload = payload;
            }

            public string Payload { get; private set; }
        }

        public class BarEvent : BaseEvent
        {
            public BarEvent(string payload)
            {
                Payload = payload;
            }

            public string Payload { get; private set; }
        }

        public class HoldEventsBasedOnModelStrategy<TEvent> : IEventHoldingStrategy<TestModel, TEvent> where TEvent : IIdentifiableEvent
        {
            public bool ShouldHold(TestModel model, TEvent @event, IEventContext context)
            {
                return model.HoldAllEvents;
            }

            public IEventDescription GetEventDescription(TestModel model, TEvent @event)
            {
                return new HeldEventDescription("Test Category", "Event being held", @event.Id);
            }
        }

        public class HoldBaseEventsBasedOnModelStrategy<TEvent, TBaseEvent> : IEventHoldingStrategy<TestModel, TEvent, TBaseEvent> where TEvent : TBaseEvent, IIdentifiableEvent
        {
            public bool ShouldHold(TestModel model, TEvent @event, IEventContext context)
            {
                return model.HoldAllEvents;
            }

            public IEventDescription GetEventDescription(TestModel model, TEvent @event)
            {
                return new HeldEventDescription("Test Category", "Event being held", @event.Id);
            }
        }

        public class HeldEventDescription : IEventDescription
        {
            public HeldEventDescription(string category, string description, Guid eventId)
            {
                Description = description;
                Category = category;
                EventId = eventId;
            }

            public Guid EventId { get; private set; }

            public string Category { get; private set; }

            public string Description { get; private set; }
        }

        [SetUp]
        public void SetUp()
        {
            _model = new TestModel();
            _router = new Router<TestModel>(_model, RouterScheduler.Default);
            _model.HoldAllEvents = true;
        }

        [Test]
        public void WhenAnEventIsHeldADiscriptionIsAddedToModel()
        {
            SetUpFooEventHoldingStrategy();
            var e = new FooEvent("EventPayload");
            _router.PublishEvent(e);
            _model.HeldEvents.Count.ShouldBe(1);
            _model.HeldEvents[0].EventId.ShouldBe(e.Id);
        }

        [Test]
        public void WhenAnEventIsHeldObserverDoesNotReceiveIt()
        {
            SetUpFooEventHoldingStrategy();
            _router.PublishEvent(new FooEvent("EventPayload"));
            _receivedFooEvents.Count.ShouldBe(0);
        }

        [Test]
        public void WhenAnEventIsRelesedTheDiscriptionIsRemovedFromTheModel()
        {
            SetUpFooEventHoldingStrategy();
            _router.PublishEvent(new FooEvent("EventPayload"));
            ReleasedEvent(_model.HeldEvents[0].EventId, HeldEventAction.Release);
            _model.HeldEvents.Count.ShouldBe(0);
        }

        [Test]
        public void WhenAnEventIsRelesedItIsPassedToTheObserver()
        {
            SetUpFooEventHoldingStrategy();
            _router.PublishEvent(new FooEvent("EventPayload"));
            ReleasedEvent(_model.HeldEvents[0].EventId, HeldEventAction.Release);
            _receivedFooEvents.Count.ShouldBe(1);
            _receivedFooEvents[0].Payload.ShouldBe("EventPayload");
        }

        [Test]
        public void MutipleEventsCanBeHeld()
        {
            SetUpFooEventHoldingStrategy();
            var event1 = new FooEvent("EventPayload1");
            var event2 = new FooEvent("EventPayload2");
            _router.PublishEvent(event1);
            _router.PublishEvent(event2);
            _model.HeldEvents.Count.ShouldBe(2);
            _model.HeldEvents[0].EventId.ShouldBe(event1.Id);
            _model.HeldEvents[1].EventId.ShouldBe(event2.Id);
        }

        [Test]
        public void MutipleEventsCanReleased()
        {
            SetUpFooEventHoldingStrategy();
            var event1 = new FooEvent("EventPayload1");
            var event2 = new FooEvent("EventPayload2");
            _router.PublishEvent(event1);
            _router.PublishEvent(event2);

            ReleasedEvent(_model.HeldEvents[1].EventId, HeldEventAction.Release);
            _receivedFooEvents.Count.ShouldBe(1);
            _receivedFooEvents[0].Payload.ShouldBe("EventPayload2");

            ReleasedEvent(_model.HeldEvents[0].EventId, HeldEventAction.Release);
            _receivedFooEvents.Count.ShouldBe(2);
            _receivedFooEvents[1].Payload.ShouldBe("EventPayload1");
        }

        [Test]
        public void IgnoreEventsAreNotReleased()
        {
            SetUpFooEventHoldingStrategy();
            var event1 = new FooEvent("EventPayload1");
            var event2 = new FooEvent("EventPayload2");
            var event3 = new FooEvent("EventPayload3");
            _router.PublishEvent(event1);
            _router.PublishEvent(event2);
            _router.PublishEvent(event3);
            ReleasedEvent(event3.Id, HeldEventAction.Ignore);
            ReleasedEvent(event1.Id, HeldEventAction.Release);
            ReleasedEvent(event2.Id, HeldEventAction.Release);
            _receivedFooEvents.Count.ShouldBe(2);
            _receivedFooEvents[0].Payload.ShouldBe("EventPayload1");
            _receivedFooEvents[1].Payload.ShouldBe("EventPayload2");
        }

        [Test]
        public void IfObservationDisposedReleasedEventNotObserved()
        {
            SetUpFooEventHoldingStrategy();
            var event1 = new FooEvent("EventPayload1");
            _router.PublishEvent(event1);
            _fooEventStreamDisposable.Dispose();
            ReleasedEvent(event1.Id, HeldEventAction.Ignore);
            _receivedFooEvents.Count.ShouldBe(0);

            // TODO note disposing the stream doesn't trash the held events on the model. Need to do something about this
            _model.HeldEvents.Count.ShouldBe(1);
        }

        [Test]
        public void CanHoldDifferingEventTypes()
        {
            SetUpFooEventHoldingStrategy();
            SetUpBarEventHoldingStrategy();
            var event1 = new FooEvent("EventPayload1");
            var event2 = new BarEvent("EventPayload2");
            _router.PublishEvent(event1);
            _router.PublishEvent(event2);
            ReleasedEvent(event1.Id, HeldEventAction.Release);
            ReleasedEvent(event2.Id, HeldEventAction.Release);
            _receivedFooEvents.Count.ShouldBe(1);
            _receivedFooEvents[0].Payload.ShouldBe("EventPayload1");
            _receivedBarEvents.Count.ShouldBe(1);
            _receivedBarEvents[0].Payload.ShouldBe("EventPayload2");
        }

        [Test]
        public void CanHoldByBaseEvent()
        {
            List<BaseEvent> receivedBarEvents = new List<BaseEvent>();
            IEventObservable<TestModel, BaseEvent, IEventContext> fooEventStream = _router.GetEventObservable(new HoldBaseEventsBasedOnModelStrategy<FooEvent, BaseEvent>());
            IEventObservable<TestModel, BaseEvent, IEventContext> barEventStream = _router.GetEventObservable(new HoldBaseEventsBasedOnModelStrategy<BarEvent, BaseEvent>());
            var stream = EventObservable.Concat(fooEventStream, barEventStream);
            stream.Observe((model, baseEvent, context) =>
            {
                receivedBarEvents.Add(baseEvent);
            });
            var event1 = new FooEvent("EventPayload1");
            var event2 = new BarEvent("EventPayload2");
            _router.PublishEvent(event1);
            _router.PublishEvent(event2);
            ReleasedEvent(event1.Id, HeldEventAction.Release);
            ReleasedEvent(event2.Id, HeldEventAction.Release);
            receivedBarEvents.Count.ShouldBe(2);
            receivedBarEvents[0].ShouldBeAssignableTo<FooEvent>();
            receivedBarEvents[1].ShouldBeAssignableTo<BarEvent>();
        }

        public void SetUpFooEventHoldingStrategy()
        {
            _receivedFooEvents = new List<FooEvent>();
            _fooEventStreamDisposable = _router.GetEventObservable(new HoldEventsBasedOnModelStrategy<FooEvent>()).Observe((m, e, c) =>
            {
                _receivedFooEvents.Add(e);
            });
        }

        public void SetUpBarEventHoldingStrategy()
        {
            _receivedBarEvents = new List<BarEvent>();
            _barEventStreamDisposable = _router.GetEventObservable(new HoldEventsBasedOnModelStrategy<BarEvent>()).Observe((m, e, c) =>
            {
                _receivedBarEvents.Add(e);
            });
        }

        private void ReleasedEvent(Guid eventId, HeldEventAction heldEventAction)
        {
            _router.PublishEvent(new HeldEventActionEvent(eventId, heldEventAction));
        }
    }
}
#endif