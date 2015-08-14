using System;
using System.Collections.Generic;
using Esp.Net.ModelRouter;
using NUnit.Framework;
using Shouldly;

namespace Esp.Net
{
    [TestFixture]
    public class BaseModelEventProcessorTests
    {
        private IRouter<TestModel> _router;
        private StubEventProcessor _stubEventProcessor;

        [SetUp]
        public void SetUp()
        {
            var router = new Router(ThreadGuard.Default);
            var modelId = Guid.NewGuid();
            router.RegisterModel(modelId, new TestModel());
            _router = router.CreateModelRouter<TestModel>(modelId);
            _stubEventProcessor = new StubEventProcessor(_router);
        }

        [Test]
        public void ObserveAttributeObservesEventsWithModelAndEventAndEventContextSignature()
        {
            _stubEventProcessor.ObserveEvents();
            _router.PublishEvent(new FooEvent());
            _stubEventProcessor.FooEvents.Count.ShouldBe(1);
            _stubEventProcessor.BarEvents.Count.ShouldBe(0);

            _router.PublishEvent(new BarEvent());
            _stubEventProcessor.FooEvents.Count.ShouldBe(1);
            _stubEventProcessor.BarEvents.Count.ShouldBe(1);
        }

        [Test]
        public void ObserveAttributeObservesEventsWithModelAndEventSignature()
        {
            _stubEventProcessor.ObserveEvents(); 
            _router.PublishEvent(new EventForNoContext());
            _stubEventProcessor.EventForNoContexts.Count.ShouldBe(1);
        }

        public class TestModel
        {
        }

        public class FooEvent
        {
        }

        public class BarEvent
        {
        }

        public class EventForNoContext
        {
        }

        public class StubEventProcessor : BaseModelEventProcessor<TestModel>
        {
            public StubEventProcessor(IRouter<TestModel> router)
                : base(router)
            {
                FooEvents = new List<Tuple<TestModel, FooEvent, IEventContext>>();
                BarEvents = new List<Tuple<TestModel, BarEvent, IEventContext>>();
                EventForNoContexts = new List<Tuple<TestModel, EventForNoContext>>();
            }

            public List<Tuple<TestModel, FooEvent, IEventContext>> FooEvents { get; private set; }

            public List<Tuple<TestModel, BarEvent, IEventContext>> BarEvents { get; private set; }

            public List<Tuple<TestModel, EventForNoContext>> EventForNoContexts { get; private set; }

            [ObserveEventAttribute(typeof(FooEvent))]
            public void ObserveFooEvent(TestModel model, FooEvent e, IEventContext context)
            {
                FooEvents.Add(Tuple.Create(model, e, context));
            }

            [ObserveEventAttribute(typeof(BarEvent), ObservationStage.Preview)]
            public void ObserveBarEvent(TestModel model, BarEvent e, IEventContext context)
            {
                BarEvents.Add(Tuple.Create(model, e, context));
            }

            [ObserveEventAttribute(typeof(EventForNoContext))]
            public void ObserveEventWithoutContext(TestModel model, EventForNoContext e)
            {
                EventForNoContexts.Add(Tuple.Create(model, e));
            }

            public void NothingOnThis()
            {
            }
        } 
    }
}