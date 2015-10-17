#region copyright
// Copyright 2015 Keith Woods
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

#if ESP_EXPERIMENTAL
using System;
using System.Collections.Generic;
using Esp.Net.Reactive;
using NUnit.Framework;
using Shouldly;

namespace Esp.Net.HeldEvents
{
    [TestFixture]
    public class HeldEventTests
    {
        private TestModel _model;
        private Router _router;
        private List<FooEvent> _receivedFooEvents;
        private IDisposable _fooEventStreamDisposable;
        private List<BarEvent> _receivedBarEvents;
        private IDisposable _barEventStreamDisposable;

        public class TestModel : IHeldEventStore
        {
            public TestModel()
            {
                HeldEvents = new List<IEventDescription>();
                Id = Guid.NewGuid();
            }

            public Guid Id { get; private set; }

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

        public class HoldEventsBasedOnModelStrategy<TEvent> : IEventHoldingStrategy<TEvent, TestModel> where TEvent : IIdentifiableEvent
        {
            public bool ShouldHold(TEvent @event, IEventContext context, TestModel model)
            {
                return model.HoldAllEvents;
            }

            public IEventDescription GetEventDescription(TEvent @event, TestModel model)
            {
                return new HeldEventDescription("Test Category", "Event being held", @event.Id);
            }
        }

        public class HoldBaseEventsBasedOnModelStrategy<TEvent, TBaseEvent> : IEventHoldingStrategy<TEvent, TBaseEvent, TestModel> where TEvent : TBaseEvent, IIdentifiableEvent
        {
            public bool ShouldHold(TEvent @event, IEventContext context, TestModel model)
            {
                return model.HoldAllEvents;
            }

            public IEventDescription GetEventDescription(TEvent @event, TestModel model)
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
            _router = new Router(new StubRouterDispatcher());
            _router.AddModel(_model.Id, _model);
            _model.HoldAllEvents = true;
        }

        [Test]
        public void WhenAnEventIsHeldADiscriptionIsAddedToModel()
        {
            SetUpFooEventHoldingStrategy();
            var e = new FooEvent("EventPayload");
            _router.PublishEvent(_model.Id, e);
            _model.HeldEvents.Count.ShouldBe(1);
            _model.HeldEvents[0].EventId.ShouldBe(e.Id);
        }

        [Test]
        public void WhenAnEventIsHeldObserverDoesNotReceiveIt()
        {
            SetUpFooEventHoldingStrategy();
            _router.PublishEvent(_model.Id, new FooEvent("EventPayload"));
            _receivedFooEvents.Count.ShouldBe(0);
        }

        [Test]
        public void WhenAnEventIsRelesedTheDiscriptionIsRemovedFromTheModel()
        {
            SetUpFooEventHoldingStrategy();
            _router.PublishEvent(_model.Id, new FooEvent("EventPayload"));
            ReleasedEvent(_model.HeldEvents[0].EventId, HeldEventAction.Release);
            _model.HeldEvents.Count.ShouldBe(0);
        }

        [Test]
        public void WhenAnEventIsRelesedItIsPassedToTheObserver()
        {
            SetUpFooEventHoldingStrategy();
            _router.PublishEvent(_model.Id, new FooEvent("EventPayload"));
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
            _router.PublishEvent(_model.Id, event1);
            _router.PublishEvent(_model.Id, event2);
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
            _router.PublishEvent(_model.Id, event1);
            _router.PublishEvent(_model.Id, event2);

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
            _router.PublishEvent(_model.Id, event1);
            _router.PublishEvent(_model.Id, event2);
            _router.PublishEvent(_model.Id, event3);
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
            _router.PublishEvent(_model.Id, event1);
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
            _router.PublishEvent(_model.Id, event1);
            _router.PublishEvent(_model.Id, event2);
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
            IEventObservable<BaseEvent, IEventContext, TestModel> fooEventStream = _router.GetEventObservable(_model.Id, new HoldBaseEventsBasedOnModelStrategy<FooEvent, BaseEvent>());
            IEventObservable<BaseEvent, IEventContext, TestModel> barEventStream = _router.GetEventObservable(_model.Id, new HoldBaseEventsBasedOnModelStrategy<BarEvent, BaseEvent>());
            var stream = EventObservable.Merge(fooEventStream, barEventStream);
            stream.Observe((baseEvent, context, model) =>
            {
                receivedBarEvents.Add(baseEvent);
            });
            var event1 = new FooEvent("EventPayload1");
            var event2 = new BarEvent("EventPayload2");
            _router.PublishEvent(_model.Id, event1);
            _router.PublishEvent(_model.Id, event2);
            ReleasedEvent(event1.Id, HeldEventAction.Release);
            ReleasedEvent(event2.Id, HeldEventAction.Release);
            receivedBarEvents.Count.ShouldBe(2);
            receivedBarEvents[0].ShouldBeAssignableTo<FooEvent>();
            receivedBarEvents[1].ShouldBeAssignableTo<BarEvent>();
        }

        [Test]
        public void CanHoldUsingModelRouter()
        {
            IRouter<TestModel> modelRouter = new Router<TestModel>(_model.Id, _router);
            IEventObservable<BaseEvent, IEventContext, TestModel> baseEventStream = modelRouter.GetEventObservable(new HoldBaseEventsBasedOnModelStrategy<FooEvent, BaseEvent>());
            int receivedBaseEvents = 0, reveivedBarEvents = 0;
            baseEventStream.Observe((model, baseEvent, context) =>
            {
                receivedBaseEvents++;
            });
            IEventObservable<BarEvent, IEventContext, TestModel> fooEventStream = modelRouter.GetEventObservable(new HoldEventsBasedOnModelStrategy<BarEvent>());
            fooEventStream.Observe((model, barEvent, context) =>
            {
                reveivedBarEvents++;
            });
            var event1 = new FooEvent("EventPayload1");
            var event2 = new BarEvent("EventPayload2");
            _router.PublishEvent(_model.Id, event1);
            _router.PublishEvent(_model.Id, event2);
            receivedBaseEvents.ShouldBe(0);
            reveivedBarEvents.ShouldBe(0);
            ReleasedEvent(event1.Id, HeldEventAction.Release);
            ReleasedEvent(event2.Id, HeldEventAction.Release);
            receivedBaseEvents.ShouldBe(1);
            reveivedBarEvents.ShouldBe(1);
        }

        public void SetUpFooEventHoldingStrategy()
        {
            _receivedFooEvents = new List<FooEvent>();
            _fooEventStreamDisposable = _router.GetEventObservable(_model.Id, new HoldEventsBasedOnModelStrategy<FooEvent>()).Observe((e, c, m) =>
            {
                _receivedFooEvents.Add(e);
            });
        }

        public void SetUpBarEventHoldingStrategy()
        {
            _receivedBarEvents = new List<BarEvent>();
            _barEventStreamDisposable = _router.GetEventObservable(_model.Id, new HoldEventsBasedOnModelStrategy<BarEvent>()).Observe((e, c, m) =>
            {
                _receivedBarEvents.Add(e);
            });
        }

        private void ReleasedEvent(Guid eventId, HeldEventAction heldEventAction)
        {
            _router.PublishEvent(_model.Id, new HeldEventActionEvent(eventId, heldEventAction));
        }
    }
}
#endif