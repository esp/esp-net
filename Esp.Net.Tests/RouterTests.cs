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
using System;
using System.Collections.Generic;
using Esp.Net.Model;
using Esp.Net.Reactive;
using Esp.Net.Router;
using NUnit.Framework;
using Shouldly;

namespace Esp.Net
{
    [TestFixture]
    public class RouterTests
    {
        public class TestModel
        {
            public TestModel()
            {
                Id = Guid.NewGuid();
            }
            public Guid Id { get; private set; }
            public int AnInt { get; set; }
            public string AString { get; set; }
            public decimal ADecimal { get; set; }
        }

        public class BaseEvent { }
        public class Event1 : BaseEvent { }
        public class Event2 : BaseEvent { }
        public class Event3 : BaseEvent { }
        public class EventWithoutBaseType { }

        private TestModel _model;

        private Router.Router _router;

        [SetUp]
        public void SetUp()
        {
            _model = new TestModel();
            _router = new Router.Router(ThreadGuard.Default);
            _router.RegisterModel(_model.Id, _model);
        }

        [Test]
        public void PublishedEventsGetDeliveredToObservers()
        {
            int deliveryCount1 = 0, deliveryCount2 = 0;
            _router.GetEventObservable<TestModel, string>(_model.Id).Observe(
                (model, e, context) => deliveryCount1++
            );
            _router.GetEventObservable<TestModel, string>(_model.Id).Observe(
                (model, e, context) => deliveryCount2++
            );
            _router.PublishEvent(_model.Id, "Foo");
            _router.PublishEvent(_model.Id, "Foo");
            deliveryCount1.ShouldBe(2);
            deliveryCount2.ShouldBe(2);
        }

        [Test]
        public void DisposingAnEventSubscriptionRemovesTheObserver()
        {
            int deliveryCount1 = 0, deliveryCount2 = 0;
            var disposable = _router.GetEventObservable<TestModel, string>(_model.Id).Observe(
                (model, e, context) => deliveryCount1++
            );
            _router.GetEventObservable<TestModel, string>(_model.Id).Observe(
                (model, e, context) => deliveryCount2++
            );
            _router.PublishEvent(_model.Id, "Foo");
            _router.PublishEvent(_model.Id, "Foo");
            deliveryCount1.ShouldBe(2);
            deliveryCount2.ShouldBe(2);
            disposable.Dispose();
            _router.PublishEvent(_model.Id, "Foo");
            _router.PublishEvent(_model.Id, "Foo");
            deliveryCount1.ShouldBe(2);
            deliveryCount2.ShouldBe(4);
        }

        [Test]
        public void PreviewEventObserversReceiveEvent()
        {
            int deliveryCount1 = 0;
            _router.GetEventObservable<TestModel, string>(_model.Id, ObservationStage.Preview).Observe(
                (model, e, context) => deliveryCount1++
            );
            _router.PublishEvent(_model.Id, "Foo");
            deliveryCount1.ShouldBe(1);
        }

        [Test] 
        public void CancelingAnEventAtPreviewStopsEventPropagation()
        {
            int previewDeliveryCount = 0, normalDeliveryCount = 0;
            _router.GetEventObservable<TestModel, string>(_model.Id, ObservationStage.Preview).Observe(
                (model, e, context) =>
                {
                    previewDeliveryCount++;
                    context.Cancel();
                }
            );
            _router.GetEventObservable<TestModel, string>(_model.Id).Observe(
                (model, e, context) =>
                {
                    normalDeliveryCount++;
                }
            );
            _router.PublishEvent(_model.Id, "Foo");
            previewDeliveryCount.ShouldBe(1);
            normalDeliveryCount.ShouldBe(0);
        }

        [Test]
        public void CommittedEventObserversReceiveEvent()
        {
            int normalDeliveryCount = 0, committedDeliveryCount1 = 0, committedDeliveryCount2 = 0;
            _router.GetEventObservable<TestModel, string>(_model.Id).Observe(
                (model, e, context) =>
                {
                    normalDeliveryCount++;
                    context.Commit();
                }
            );
            _router.GetEventObservable<TestModel, string>(_model.Id, ObservationStage.Committed).Observe(
                (model, e, context) =>
                {
                    committedDeliveryCount1++;
                }
            );
            _router.GetEventObservable<TestModel, string>(_model.Id, ObservationStage.Committed).Observe(
                (model, e, context) =>
                {
                    committedDeliveryCount2++;
                }
            );
            _router.PublishEvent(_model.Id, "Foo");
            normalDeliveryCount.ShouldBe(1);
            committedDeliveryCount1.ShouldBe(1);
            committedDeliveryCount2.ShouldBe(1);
            _router.PublishEvent(_model.Id, "Foo");
            normalDeliveryCount.ShouldBe(2);
            committedDeliveryCount1.ShouldBe(2);
            committedDeliveryCount2.ShouldBe(2);
        }

        [Test]
        public void PublishedEventsGetProcessedInTurn()
        {
            int event1Count = 0, event2Count = 0;
            bool testPassed = false;
            _router.GetEventObservable<TestModel, string>(_model.Id).Observe(
                (model, e, context) =>
                {
                    _router.PublishEvent(_model.Id, 1);
                    event1Count++;
                    context.Commit();
                }
            );
            _router.GetEventObservable<TestModel, string>(_model.Id, ObservationStage.Committed).Observe(
                (model, e, context) =>
                {
                    testPassed = event1Count == 1 && event2Count == 0;
                }
            );
            _router.GetEventObservable<TestModel, int>(_model.Id).Observe(
                (model, e, context) =>
                {
                    event2Count++;
                    context.Commit();
                }
            );
            _router.PublishEvent(_model.Id, "foo");
            testPassed = testPassed && event2Count == 1;
            testPassed.ShouldBe(true);
        }

        [Test]
        public void CanObserveEventsByBaseType()
        {
            // this is effictively what we're doing here with covariance  
            // IEventContext<TestModel, BaseEvent> ec1 = new EventContext<TestModel, Event1>(null, null);
            // For example check out the .Observe() method, it receives an event context 
            // typed for BaseEvent not Event1.
            var receivedEvents1 = new List<BaseEvent>();
            var receivedEvents2 = new List<BaseEvent>();
            _router.GetEventObservable<TestModel, BaseEvent>(_model.Id, typeof(Event1))
                .Observe((model, baseEvent, context) =>
                {
                    receivedEvents1.Add(baseEvent);
                });
            _router.GetEventObservable<TestModel, Event1, BaseEvent>(_model.Id)
                .Observe((model, baseEvent, context) =>
                {
                    receivedEvents2.Add(baseEvent);
                });
            _router.PublishEvent(_model.Id, new Event1());
            receivedEvents1.Count.ShouldBe(1);
            receivedEvents2.Count.ShouldBe(1);
        }

        [Test]
        public void WhenObservingByBaseTypeItThrowsIfSubTypeDoesntDeriveFromBase()
        {
            Assert.Throws<InvalidOperationException>(() => {
                _router.GetEventObservable<TestModel, BaseEvent>(_model.Id, typeof(EventWithoutBaseType)).Observe((m, e, c) => { });
            });
        }

        [Test]
        public void CanConcatEventStreams()
        {
            var receivedEvents = new List<BaseEvent>();
            var stream = EventObservable.Concat(
                _router.GetEventObservable<TestModel, BaseEvent>(_model.Id, typeof(Event1)),
                _router.GetEventObservable<TestModel, BaseEvent>(_model.Id, typeof(Event2)),
                _router.GetEventObservable<TestModel, BaseEvent>(_model.Id, typeof(Event3))
            );
            stream.Observe((model, baseEvent, context) =>
            {
                receivedEvents.Add(baseEvent);
            });
            _router.PublishEvent(_model.Id, new Event1());
            _router.PublishEvent(_model.Id, new Event2());
            _router.PublishEvent(_model.Id, new Event3());
            receivedEvents.Count.ShouldBe(3);
        }

        [Test]
        public void OnPublishExceptionsBubbleUp()
        {
            _router.GetEventObservable<TestModel, string>(_model.Id).Observe(
                (model, e, context) =>
                {
                   throw new InvalidOperationException("POP");
                }
            );

            Should.Throw<InvalidOperationException>(() => _router.PublishEvent(_model.Id, "foo"));
        }

        [Test]
        public void OnceHaltedSubsequentEventPublicationThrows()
        {
            _router.GetEventObservable<TestModel, string>(_model.Id).Observe(
                (model, e, context) =>
                {
                    throw new AccessViolationException("POP");
                }
            );

            Should.Throw<AccessViolationException>(() => _router.PublishEvent(_model.Id, "foo"));
            Should.Throw<AccessViolationException>(() => _router.PublishEvent(_model.Id, "foo"));
        }

        [Test]
        public void ModelGetsDispatchedAfterEventsProcessed()
        {
            int updateCount = 0;
            _router.GetEventObservable<TestModel, int>(_model.Id).Observe(
                (model, e, context) =>
                {
                    model.AnInt = e;
                    _router.PublishEvent(_model.Id, "pew pew");
                }
            );
            _router.GetEventObservable<TestModel, string>(_model.Id).Observe(
                (model, e, context) =>
                {
                    model.AString = e;
                    _router.PublishEvent(_model.Id, 1.1m);
                }
            );
            _router.GetEventObservable<TestModel, decimal>(_model.Id).Observe(
                (model, e, context) =>
                {
                    model.ADecimal = e;
                }
            );

            TestModel resultModel = null;
            _router.GetModelObservable<TestModel>(_model.Id).Observe(
               model =>
               {
                   updateCount++;
                   resultModel = model;
               }
            );
            _router.PublishEvent(_model.Id, 1);
            updateCount.ShouldBe(1);
            resultModel.ShouldSatisfyAllConditions(
                () => resultModel.AString.ShouldBe("pew pew"),
                () => resultModel.AnInt.ShouldBe(1),
                () => resultModel.ADecimal.ShouldBe(1.1m));
        }

        [Test]
        public void EventsPublishedDuringPreEventProcessingGetProcessed()
        {
            // note we don't use the locals setup for other tests here
            List<int> receivedEvents = new List<int>();
            var model = new TestModel();
            var modelUpdateCount = 0;
            Router.Router router = null;
            var preEventProcessor = new DelegeatePreEventProcessor(m =>
            {
                router.PublishEvent(_model.Id, 1);
            });
            router = new Router.Router(ThreadGuard.Default);
            router.RegisterModel(model.Id, model, preEventProcessor);
            router.GetEventObservable<TestModel, int>(_model.Id).Observe((m, e, c) => {
                receivedEvents.Add(e);
            });
            router.GetModelObservable<TestModel>(_model.Id).Observe(model1 => {
                modelUpdateCount++;
            });
            router.PublishEvent(_model.Id, 0);
            receivedEvents.ShouldBe(new[]{ 0, 1});
            modelUpdateCount.ShouldBe(1);
        }

        [Test]
        public void EventsPublishedDuringPostEventProcessingGetProcessed()
        {
            // note we don't use the locals setup for other tests here
            List<int> receivedEvents = new List<int>();
            var model = new TestModel();
            var modelUpdateCount = 0;
            Router.Router router = null;
            var postEventProcessor = new DelegeatePostEventProcessor(m =>
            {
                if (m.AnInt < 1)
                {
                    router.PublishEvent(_model.Id, 1);
                }
            });
            router = new Router.Router(ThreadGuard.Default);
            router.RegisterModel(model.Id, model, postEventProcessor);
            router.GetEventObservable<TestModel, int>(_model.Id).Observe((m, e, c) => {
                m.AnInt = e;
                receivedEvents.Add(e);
            });
            router.GetModelObservable<TestModel>(_model.Id).Observe(
               model1 =>
               {
                   modelUpdateCount++;
               }
            );
            router.PublishEvent(_model.Id, 0);
            receivedEvents.ShouldBe(new[] { 0, 1 });
            modelUpdateCount.ShouldBe(1);
        }

        [Test]
        public void EventsPublishedDuringPostEventProcessingCauseANewEventProcessingCycle()
        {
            // bit of a crazy test but effictively it tests that when an event is published by a post event processor 
            // it re-runs the internal workflow of 'preProcessor -> eventDispatch -> postProcessor' and it does this 
            // without procuring another model tick.

            // note we don't use the locals setup for other tests here
            var receivedEvents = new List<int>();
            var model = new TestModel();
            int preProcessorCount = 0, postProcessorCount = 0, modelCount = 0;
            Router.Router router = null;
            var preEventProcessor = new DelegeatePreEventProcessor(m =>
            {
                preProcessorCount++;
                if (m.AnInt == 0)
                {
                    router.PublishEvent(_model.Id, 1);
                }
            });
            var postEventProcessor = new DelegeatePostEventProcessor(m =>
            {
                postProcessorCount++;
                if (m.AnInt == 1)
                {
                    router.PublishEvent(_model.Id, 2);
                }
            });
            router = new Router.Router(ThreadGuard.Default);
            router.RegisterModel(model.Id, model, preEventProcessor, postEventProcessor);
            router.GetEventObservable<TestModel, int>(_model.Id).Observe((m, e, c) =>
            {
                m.AnInt = e;
                receivedEvents.Add(e);
            });
            router.GetModelObservable<TestModel>(_model.Id).Observe(
               model1 =>
               {
                   modelCount++;
               }
            );
            router.PublishEvent(_model.Id, 0);
            receivedEvents.ShouldBe(new[] { 0, 1, 2 });
            modelCount.ShouldBe(1);
            preProcessorCount.ShouldBe(2);
            postProcessorCount.ShouldBe(2);
        }

        [Test]
        public void EventsPublishedDuringModelUpdateDispatchGetProcessed()
        {
            int updateCount = 0;
            _router.GetEventObservable<TestModel, int>(_model.Id).Observe((m, e, c) => {
                m.AnInt = e;
            });
            _router.GetModelObservable<TestModel>(_model.Id).Observe(
               model =>
               {
                   updateCount++;
                   if (model.AnInt == 0)
                   {
                       _router.PublishEvent(_model.Id, 1);
                   }
               }
           );
            _router.PublishEvent(_model.Id, 0);
            updateCount.ShouldBe(2);
        }

        public class DelegeatePreEventProcessor : IPreEventProcessor<TestModel>
        {
            private readonly Action<TestModel> _process;

            public DelegeatePreEventProcessor(Action<TestModel> process)
            {
                _process = process;
            }

            public void Process(TestModel model)
            {
                _process(model);
            }
        }

        public class DelegeatePostEventProcessor : IPostEventProcessor<TestModel>
        {
            private readonly Action<TestModel> _process;

            public DelegeatePostEventProcessor(Action<TestModel> process)
            {
                _process = process;
            }

            public void Process(TestModel model)
            {
                _process(model);
            }
        }
    }
}