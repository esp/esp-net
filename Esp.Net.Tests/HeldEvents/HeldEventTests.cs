using System;
using System.Collections.Generic;
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
        private List<FooEvent> _receivedEvents;
        private IDisposable _eventStreamDisposable;

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

        public class FooEvent : IIdentifiableEvent
        {
            public FooEvent(string payload)
            {
                Payload = payload;
                Id = Guid.NewGuid();
            }

            public string Payload { get; private set; }

            public Guid Id { get; private set; }
        }

        public class FooEventHoldingStrategy : IEventHoldingStrategy<TestModel, FooEvent>
        {
            public bool ShouldHold(TestModel model, FooEvent @event, IEventContext context)
            {
                return model.HoldAllEvents;
            }

            public IEventDescription GetEventDescription(TestModel model, FooEvent @event)
            {
                return new HeldEventDescription("Test Category", "Foo event being held", @event.Id);
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
            _receivedEvents = new List<FooEvent>();
            _eventStreamDisposable =  _router.GetEventObservable(new FooEventHoldingStrategy()).Observe((m, e, c) =>
            {
                _receivedEvents.Add(e);
            });
            _model.HoldAllEvents = true;
        }

        [Test]
        public void WhenAnEventIsHeldADiscriptionIsAddedToModel()
        {
            var e = new FooEvent("EventPayload");
            _router.PublishEvent(e);
            _model.HeldEvents.Count.ShouldBe(1);
            _model.HeldEvents[0].EventId.ShouldBe(e.Id);
        }

        [Test]
        public void WhenAnEventIsHeldObserverDoesNotReceiveIt()
        {
            _router.PublishEvent(new FooEvent("EventPayload"));
            _receivedEvents.Count.ShouldBe(0);
        }

        [Test]
        public void WhenAnEventIsRelesedTheDiscriptionIsRemovedFromTheModel()
        {
            _router.PublishEvent(new FooEvent("EventPayload"));
            ReleasedEvent(_model.HeldEvents[0].EventId, HeldEventAction.Release);
            _model.HeldEvents.Count.ShouldBe(0);
        }

        [Test]
        public void WhenAnEventIsRelesedItIsPassedToTheObserver()
        {
            _router.PublishEvent(new FooEvent("EventPayload"));
            ReleasedEvent(_model.HeldEvents[0].EventId, HeldEventAction.Release);
            _receivedEvents.Count.ShouldBe(1);
            _receivedEvents[0].Payload.ShouldBe("EventPayload");
        }

        [Test]
        public void MutipleEventsCanBeHeld()
        {
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
            var event1 = new FooEvent("EventPayload1");
            var event2 = new FooEvent("EventPayload2");
            _router.PublishEvent(event1);
            _router.PublishEvent(event2);

            ReleasedEvent(_model.HeldEvents[1].EventId, HeldEventAction.Release);
            _receivedEvents.Count.ShouldBe(1);
            _receivedEvents[0].Payload.ShouldBe("EventPayload2");

            ReleasedEvent(_model.HeldEvents[0].EventId, HeldEventAction.Release);
            _receivedEvents.Count.ShouldBe(2);
            _receivedEvents[1].Payload.ShouldBe("EventPayload1");
        }

        [Test]
        public void IgnoreEventsAreNotReleased()
        {
            var event1 = new FooEvent("EventPayload1");
            var event2 = new FooEvent("EventPayload2");
            var event3 = new FooEvent("EventPayload3");
            _router.PublishEvent(event1);
            _router.PublishEvent(event2);
            _router.PublishEvent(event3);
            ReleasedEvent(event3.Id, HeldEventAction.Ignore);
            ReleasedEvent(event1.Id, HeldEventAction.Release);
            ReleasedEvent(event2.Id, HeldEventAction.Release);
            _receivedEvents.Count.ShouldBe(2);
            _receivedEvents[0].Payload.ShouldBe("EventPayload1");
            _receivedEvents[1].Payload.ShouldBe("EventPayload2");
        }

        [Test]
        public void IfObservationDisposedReleasedEventNotObserved()
        {
            var event1 = new FooEvent("EventPayload1");
            _router.PublishEvent(event1);
            _eventStreamDisposable.Dispose();
            ReleasedEvent(event1.Id, HeldEventAction.Ignore);
            _receivedEvents.Count.ShouldBe(0);

            // TODO note disposing the stream doesn't trash the held events on the model. Need to do something about this
            _model.HeldEvents.Count.ShouldBe(1);
        }

        private void ReleasedEvent(Guid eventId, HeldEventAction heldEventAction)
        {
            _router.PublishEvent(new HeldEventActionEvent(eventId, heldEventAction));
        }
    }
}
#endif