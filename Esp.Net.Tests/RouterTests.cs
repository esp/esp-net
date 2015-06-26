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
using Esp.Net.Reactive;
using NUnit.Framework;
using Shouldly;

namespace Esp.Net
{
    [TestFixture]
    public class RouterTests
    {
        public class TestModel
        {
            public int AnInt { get; set; }
            public string AString { get; set; }
            public decimal ADecimal { get; set; }
        }

        public class BaseEvent { }
        public class Event1 : BaseEvent { }
        public class Event2 : BaseEvent { }
        public class Event3 : BaseEvent { }

        private TestModel _model;

        private Router<TestModel> _router;

        [SetUp]
        public void SetUp()
        {
            _model = new TestModel();
            _router = new Router<TestModel>(_model, RouterScheduler.Default);
        }

        [Test]
        public void PublishedEventsGetDeliveredToObservers()
        {
            int deliveryCount1 = 0, deliveryCount2 = 0;
            _router.GetEventObservable<string>().Observe(
                context => deliveryCount1++
            );
            _router.GetEventObservable<string>().Observe(
                context => deliveryCount2++
            );
            _router.PublishEvent("Foo");
            _router.PublishEvent("Foo");
            deliveryCount1.ShouldBe(2);
            deliveryCount2.ShouldBe(2);
        }

        [Test]
        public void DisposingAnEventSubscriptionRemovesTheObserver()
        {
            int deliveryCount1 = 0, deliveryCount2 = 0;
            var disposable = _router.GetEventObservable<string>().Observe(
                context => deliveryCount1++
            );
            _router.GetEventObservable<string>().Observe(
                context => deliveryCount2++
            );
            _router.PublishEvent("Foo");
            _router.PublishEvent("Foo");
            deliveryCount1.ShouldBe(2);
            deliveryCount2.ShouldBe(2);
            disposable.Dispose();
            _router.PublishEvent("Foo");
            _router.PublishEvent("Foo");
            deliveryCount1.ShouldBe(2);
            deliveryCount2.ShouldBe(4);
        }

        [Test]
        public void PreviewEventObserversReceiveEvent()
        {
            int deliveryCount1 = 0;
            _router.GetEventObservable<string>(ObservationStage.Preview).Observe(
                context => deliveryCount1++
            );
            _router.PublishEvent("Foo");
            deliveryCount1.ShouldBe(1);
        }

        [Test] 
        public void CancelingAnEventAtPreviewStopsEventPropagation()
        {
            int previewDeliveryCount = 0, normalDeliveryCount = 0;
            _router.GetEventObservable<string>(ObservationStage.Preview).Observe(
                context =>
                {
                    previewDeliveryCount++;
                    context.Cancel();
                }
            );
            _router.GetEventObservable<string>().Observe(
                context =>
                {
                    normalDeliveryCount++;
                }
            );
            _router.PublishEvent("Foo");
            previewDeliveryCount.ShouldBe(1);
            normalDeliveryCount.ShouldBe(0);
        }

        [Test]
        public void CommittedEventObserversReceiveEvent()
        {
            int normalDeliveryCount = 0, committedDeliveryCount1 = 0, committedDeliveryCount2 = 0;
            _router.GetEventObservable<string>().Observe(
                context =>
                {
                    normalDeliveryCount++;
                    context.Commit();
                }
            );
            _router.GetEventObservable<string>(ObservationStage.Committed).Observe(
                context =>
                {
                    committedDeliveryCount1++;
                }
            );
            _router.GetEventObservable<string>(ObservationStage.Committed).Observe(
                context =>
                {
                    committedDeliveryCount2++;
                }
            );
            _router.PublishEvent("Foo");
            normalDeliveryCount.ShouldBe(1);
            committedDeliveryCount1.ShouldBe(1);
            committedDeliveryCount2.ShouldBe(1);
            _router.PublishEvent("Foo");
            normalDeliveryCount.ShouldBe(2);
            committedDeliveryCount1.ShouldBe(2);
            committedDeliveryCount2.ShouldBe(2);
        }

        [Test]
        public void PublishedEventsGetProcessedInTurn()
        {
            int event1Count = 0, event2Count = 0;
            bool testPassed = false;
            _router.GetEventObservable<string>().Observe(
                context =>
                {
                    _router.PublishEvent(1);
                    event1Count++;
                    context.Commit();
                }
            );
            _router.GetEventObservable<string>(ObservationStage.Committed).Observe(
                context =>
                {
                    testPassed = event1Count == 1 && event2Count == 0;
                }
            );
            _router.GetEventObservable<int>().Observe(
                context =>
                {
                    event2Count++;
                    context.Commit();
                }
            );
            _router.PublishEvent("foo");
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
            var receivedEvents = new List<BaseEvent>();
            _router.GetEventObservable<BaseEvent>(typeof(Event1))
                .Observe((IEventContext<TestModel, BaseEvent> context) =>
                {
                    receivedEvents.Add(context.Event);
                });
            _router.PublishEvent(new Event1());
            receivedEvents.Count.ShouldBe(1);
        }

        [Test]
        public void CanConcatEventStreams()
        {
            var receivedEvents = new List<BaseEvent>();
            var stream = EventObservable.Concat(
                _router.GetEventObservable<BaseEvent>(typeof(Event1)),
                _router.GetEventObservable<BaseEvent>(typeof(Event2)),
                _router.GetEventObservable<BaseEvent>(typeof(Event3))
            );
            stream.Observe((IEventContext<TestModel, BaseEvent> context) =>
            {
                receivedEvents.Add(context.Event);
            });
            _router.PublishEvent(new Event1());
            _router.PublishEvent(new Event2());
            _router.PublishEvent(new Event3());
            receivedEvents.Count.ShouldBe(3);
        }

        [Test]
        public void OnPublishExceptionsBubbleUp()
        {
            _router.GetEventObservable<string>().Observe(
                context =>
                {
                   throw new InvalidOperationException("POP");
                }
            );

            Should.Throw<InvalidOperationException>(() => _router.PublishEvent("foo"));
        }

        [Test]
        public void OnceHaltedSubsequentEventPublicationThrows()
        {
            _router.GetEventObservable<string>().Observe(
                context =>
                {
                    throw new AccessViolationException("POP");
                }
            );

            Should.Throw<AccessViolationException>(() => _router.PublishEvent("foo"));
            Should.Throw<AccessViolationException>(() => _router.PublishEvent("foo"));
        }

        [Test]
        public void ModelGetsDispatchedAfterEventsProcessed()
        {
            int updateCount = 0;
            _router.GetEventObservable<int>().Observe(
                context =>
                {
                    context.Model.AnInt = context.Event;
                    _router.PublishEvent("pew pew");
                }
            );
            _router.GetEventObservable<string>().Observe(
                context =>
                {
                    context.Model.AString = context.Event;
                    _router.PublishEvent(1.1m);
                }
            );
            _router.GetEventObservable<decimal>().Observe(
                context =>
                {
                    context.Model.ADecimal = context.Event;
                }
            );

            TestModel resultModel = null;
            _router.GetModelObservable().Observe(
               model =>
               {
                   updateCount++;
                   resultModel = model;
               }
            );
            _router.PublishEvent(1);
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
            Router<TestModel> router = null;
            var preEventProcessor = new DelegeatePreEventProcessor(m =>
            {
                router.PublishEvent(1);
            });
            router = new Router<TestModel>(model, RouterScheduler.Default, preEventProcessor);
            router.GetEventObservable<int>().Observe(context =>
            {
                receivedEvents.Add(context.Event);
            });
            router.GetModelObservable().Observe(
               model1 =>
               {
                   modelUpdateCount++;
               }
            );
            router.PublishEvent(0);
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
            Router<TestModel> router = null;
            var postEventProcessor = new DelegeatePostEventProcessor(m =>
            {
                if (m.AnInt < 1)
                {
                    router.PublishEvent(1);
                }
            });
            router = new Router<TestModel>(model, RouterScheduler.Default, postEventProcessor);
            router.GetEventObservable<int>().Observe(context =>
            {
                context.Model.AnInt = context.Event;
                receivedEvents.Add(context.Event);
            });
            router.GetModelObservable().Observe(
               model1 =>
               {
                   modelUpdateCount++;
               }
            );
            router.PublishEvent(0);
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
            Router<TestModel> router = null;
            var preEventProcessor = new DelegeatePreEventProcessor(m =>
            {
                preProcessorCount++;
                if (m.AnInt == 0)
                {
                    router.PublishEvent(1);
                }
            });
            var postEventProcessor = new DelegeatePostEventProcessor(m =>
            {
                postProcessorCount++;
                if (m.AnInt == 1)
                {
                    router.PublishEvent(2);
                }
            });
            router = new Router<TestModel>(model, RouterScheduler.Default, preEventProcessor, postEventProcessor);
            router.GetEventObservable<int>().Observe(context =>
            {
                model.AnInt = context.Event;
                receivedEvents.Add(context.Event);
            });
            router.GetModelObservable().Observe(
               model1 =>
               {
                   modelCount++;
               }
            );
            router.PublishEvent(0);
            receivedEvents.ShouldBe(new[] { 0, 1, 2 });
            modelCount.ShouldBe(1);
            preProcessorCount.ShouldBe(2);
            postProcessorCount.ShouldBe(2);
        }

        [Test]
        public void EventsPublishedDuringModelUpdateDispatchGetProcessed()
        {
            int updateCount = 0;
            _router.GetEventObservable<int>().Observe(context =>
            {
                context.Model.AnInt = context.Event;
            });
            _router.GetModelObservable().Observe(
               model =>
               {
                   updateCount++;
                   if (model.AnInt == 0)
                   {
                       _router.PublishEvent(1);
                   }
               }
           );
            _router.PublishEvent(0);
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