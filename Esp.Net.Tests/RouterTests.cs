using System;
using System.Collections.Generic;
using System.Linq;
using Esp.Net.Reactive;
using NUnit.Framework;

namespace Esp.Net
{
    [TestFixture]
    public class RouterTests
    {
        private TestModel _model;

        private Router<TestModel> _router;

        [SetUp]
        public void SetUp()
        {
            _model = new TestModel();
            _router = new Router<TestModel>(_model, RouterScheudler.Default);
        }

        [Test]
        public void PublishedEventsGetDeliveredToObservers()
        {
            int deliveryCount1 = 0, deliveryCount2 = 0;
            _router.GetEventStream<string>().Observe(
                context => deliveryCount1++
            );
            _router.GetEventStream<string>().Observe(
                context => deliveryCount2++
            );
            _router.Publish("Foo");
            _router.Publish("Foo");
            Assert.AreEqual(2, deliveryCount1);
            Assert.AreEqual(2, deliveryCount2);
        }

        [Test]
        public void DisposingAnEventSubscriptionRemovesTheObserver()
        {
            int deliveryCount1 = 0, deliveryCount2 = 0;
            var disposable = _router.GetEventStream<string>().Observe(
                context => deliveryCount1++
            );
            _router.GetEventStream<string>().Observe(
                context => deliveryCount2++
            );
            _router.Publish("Foo");
            _router.Publish("Foo");
            Assert.AreEqual(2, deliveryCount1);
            Assert.AreEqual(2, deliveryCount2);
            disposable.Dispose();
            _router.Publish("Foo");
            _router.Publish("Foo");
            Assert.AreEqual(2, deliveryCount1);
            Assert.AreEqual(4, deliveryCount2);
        }

        [Test]
        public void PreviewEventObserversReceiveEvent()
        {
            int deliveryCount1 = 0;
            _router.GetEventStream<string>(ObservationStage.Preview).Observe(
                context => deliveryCount1++
            );
            _router.Publish("Foo");
            Assert.AreEqual(1, deliveryCount1);
        }

        [Test] 
        public void CancelingAnEventAtPreviewStopsEventPropagation()
        {
            int previewDeliveryCount = 0, normalDeliveryCount = 0;
            _router.GetEventStream<string>(ObservationStage.Preview).Observe(
                context =>
                {
                    previewDeliveryCount++;
                    context.Cancel();
                }
            );
            _router.GetEventStream<string>().Observe(
                context =>
                {
                    normalDeliveryCount++;
                }
            );
            _router.Publish("Foo");
            Assert.AreEqual(1, previewDeliveryCount);
            Assert.AreEqual(0, normalDeliveryCount);
        }

        [Test]
        public void CommittedEventObserversReceiveEvent()
        {
            int normalDeliveryCount = 0, committedDeliveryCount1 = 0, committedDeliveryCount2 = 0;
            _router.GetEventStream<string>().Observe(
                context =>
                {
                    normalDeliveryCount++;
                    context.Commit();
                }
            );
            _router.GetEventStream<string>(ObservationStage.Committed).Observe(
                context =>
                {
                    committedDeliveryCount1++;
                }
            );
            _router.GetEventStream<string>(ObservationStage.Committed).Observe(
                context =>
                {
                    committedDeliveryCount2++;
                }
            );
            _router.Publish("Foo");
            Assert.AreEqual(1, normalDeliveryCount);
            Assert.AreEqual(1, committedDeliveryCount1);
            Assert.AreEqual(1, committedDeliveryCount2);
            _router.Publish("Foo");
            Assert.AreEqual(2, normalDeliveryCount);
            Assert.AreEqual(2, committedDeliveryCount1);
            Assert.AreEqual(2, committedDeliveryCount2);
        }

        [Test]
        public void PublishedEventsGetProcessedInTurn()
        {
            int event1Count = 0, event2Count = 0;
            bool testPassed = false;
            _router.GetEventStream<string>().Observe(
                context =>
                {
                    _router.Publish(1);
                    event1Count++;
                    context.Commit();
                }
            );
            _router.GetEventStream<string>(ObservationStage.Committed).Observe(
                context =>
                {
                    testPassed = event1Count == 1 && event2Count == 0;
                }
            );
            _router.GetEventStream<int>().Observe(
                context =>
                {
                    event2Count++;
                    context.Commit();
                }
            );
            _router.Publish("foo");
            testPassed = testPassed && event2Count == 1;
            Assert.IsTrue(testPassed);
        }

        [Test]
        public void CanObserveEventsByBaseType()
        {
            // this is effictively what we're doing here with covariance  
            // IEventContext<TestModel, BaseEvent> ec1 = new EventContext<TestModel, Event1>(null, null);
            // For example check out the .Observe() method, it receives an event context 
            // typed for BaseEvent not Event1.
            List<BaseEvent> receivedEvents = new List<BaseEvent>();
            _router.GetEventStream<BaseEvent>(typeof(Event1))
                .Observe((IEventContext<TestModel, BaseEvent> context) =>
                {
                    receivedEvents.Add(context.Event);
                });
            _router.Publish(new Event1());
            Assert.AreEqual(1, receivedEvents.Count);
        }

        [Test]
        public void CanConcatEventStreams()
        {
            List<BaseEvent> receivedEvents = new List<BaseEvent>();
            var stream = EventObservable.Concat(
                _router.GetEventStream<BaseEvent>(typeof(Event1)),
                _router.GetEventStream<BaseEvent>(typeof(Event2)),
                _router.GetEventStream<BaseEvent>(typeof(Event3))
            );
            stream.Observe((IEventContext<TestModel, BaseEvent> context) =>
            {
                receivedEvents.Add(context.Event);
            });
            _router.Publish(new Event1());
            _router.Publish(new Event2());
            _router.Publish(new Event3());
            Assert.AreEqual(3, receivedEvents.Count);
        }

        [Test]
        public void OnPublishExceptionsBubbleUp()
        {
            bool exceptionThrown = false;
            _router.GetEventStream<string>().Observe(
                context =>
                {
                   throw new InvalidOperationException("POP");
                }
            );
            try
            {
                _router.Publish("foo");
            }
            catch (InvalidOperationException)
            {
                exceptionThrown = true;
            }
            Assert.IsTrue(exceptionThrown);
        }

        [Test]
        public void OnceHaltedSubsequentEventPublicationThrows()
        {
            bool exceptionRethrown = false;
            _router.GetEventStream<string>().Observe(
                context =>
                {
                    throw new AccessViolationException("POP");
                }
            );
            try
            {
                _router.Publish("foo");
            }
            catch (Exception)
            {
                // ignored
            }
            try
            {
                _router.Publish("foo");
            }
            catch (AccessViolationException)
            {
                exceptionRethrown = true;
            }
            Assert.IsTrue(exceptionRethrown);
        }

        [Test]
        public void ModelGetsDispatchedAfterEventsProcessed()
        {
            bool testPassed = false;
            int updateCount = 0;
            _router.GetEventStream<int>().Observe(
                context =>
                {
                    context.Model.AnInt = context.Event;
                    _router.Publish("pew pew");
                }
            );
            _router.GetEventStream<string>().Observe(
                context =>
                {
                    context.Model.AString = context.Event;
                    _router.Publish(1.1m);
                }
            );
            _router.GetEventStream<decimal>().Observe(
                context =>
                {
                    context.Model.ADecimal = context.Event;
                }
            );
            _router.GetModelStream().Observe(
               model =>
               {
                   updateCount++;
                   testPassed = model.AString == "pew pew" && model.AnInt == 1 && model.ADecimal == 1.1m;
               }
            );
            _router.Publish(1);
            Assert.AreEqual(1, updateCount);
            Assert.IsTrue(testPassed);
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
                router.Publish(1);
            });
            router = new Router<TestModel>(model, RouterScheudler.Default, preEventProcessor);
            router.GetEventStream<int>().Observe(context =>
            {
                receivedEvents.Add(context.Event);
            });
            router.GetModelStream().Observe(
               model1 =>
               {
                   modelUpdateCount++;
               }
            );
            router.Publish(0);
            Assert.IsTrue(receivedEvents.SequenceEqual(new[]{ 0, 1}));
            Assert.AreEqual(1, modelUpdateCount);
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
                    router.Publish(1);
                }
            });
            router = new Router<TestModel>(model, RouterScheudler.Default, postEventProcessor);
            router.GetEventStream<int>().Observe(context =>
            {
                context.Model.AnInt = context.Event;
                receivedEvents.Add(context.Event);
            });
            router.GetModelStream().Observe(
               model1 =>
               {
                   modelUpdateCount++;
               }
            );
            router.Publish(0);
            Assert.IsTrue(receivedEvents.SequenceEqual(new[] { 0, 1 }));
            Assert.AreEqual(1, modelUpdateCount);
        }

        [Test]
        public void EventsPublishedDuringPostEventProcessingCauseANewEventProcessingCycle()
        {
            // bit of a crazy test but effictively it tests that when an event is published by a post event processor 
            // it re-runs the internal workflow of 'preProcessor -> eventDispatch -> postProcessor' and it does this 
            // without procuring another model tick.

            // note we don't use the locals setup for other tests here
            List<int> receivedEvents = new List<int>();
            var model = new TestModel();
            int preProcessorCount = 0, postProcessorCount = 0, modelCount = 0;
            Router<TestModel> router = null;
            var preEventProcessor = new DelegeatePreEventProcessor(m =>
            {
                preProcessorCount++;
                if (m.AnInt == 0)
                {
                    router.Publish(1);
                }
            });
            var postEventProcessor = new DelegeatePostEventProcessor(m =>
            {
                postProcessorCount++;
                if (m.AnInt == 1)
                {
                    router.Publish(2);
                }
            });
            router = new Router<TestModel>(model, RouterScheudler.Default, preEventProcessor, postEventProcessor);
            router.GetEventStream<int>().Observe(context =>
            {
                model.AnInt = context.Event;
                receivedEvents.Add(context.Event);
            });
            router.GetModelStream().Observe(
               model1 =>
               {
                   modelCount++;
               }
            );
            router.Publish(0);
            Assert.IsTrue(receivedEvents.SequenceEqual(new[] { 0, 1, 2 }));
            Assert.AreEqual(1, modelCount);
            Assert.AreEqual(2, preProcessorCount);
            Assert.AreEqual(2, postProcessorCount);
        }

        [Test]
        public void EventsPublishedDuringModelUpdateDispatchGetProcessed()
        {
            int updateCount = 0;
            _router.GetEventStream<int>().Observe(context =>
            {
                context.Model.AnInt = context.Event;
            });
            _router.GetModelStream().Observe(
               model =>
               {
                   updateCount++;
                   if (model.AnInt == 0)
                   {
                       _router.Publish(1);
                   }
               }
           );
            _router.Publish(0);
            Assert.AreEqual(2, updateCount);
        }

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