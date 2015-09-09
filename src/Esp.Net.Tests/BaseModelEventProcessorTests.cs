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
            var router = new Router(new StubRouterDispatcher());
            var modelId = Guid.NewGuid();
            router.RegisterModel(modelId, new TestModel());
            _router = router.CreateModelRouter<TestModel>(modelId);
            _stubEventProcessor = new StubEventProcessor(_router);
        }

        [Test]
        public void ObserveAttribute_ObservesEventsWithHandler_Model_Event_Context()
        {
            _router.PublishEvent(new EventsForHandlerWith_Model_Event_Context_Evt());
            _stubEventProcessor.EventsWith_Model_Event_Context.Count.ShouldBe(1);
        }

        [Test]
        public void ObserveAttribute_ObservesEventsWithHandler_Model_Event()
        {
            _router.PublishEvent(new EventsForHandlerWith_Model_Event_Evt());
            _stubEventProcessor.EventsWith_Model_Event.Count.ShouldBe(1);
        }

        [Test]
        public void ObserveAttribute_ObservesEventsWithHandler_Model()
        {
            _router.PublishEvent(new EventsForHandlerWith_Model_Evt());
            _stubEventProcessor.EventsWith_Model.Count.ShouldBe(1);
        }

        [Test]
        public void ObserveAttribute_ObservesEventsWithHandler_Event()
        {
            _router.PublishEvent(new EventsForHandlerWith_Event_Evt());
            _stubEventProcessor.EventsWith_Event.Count.ShouldBe(1);
        }

        [Test]
        public void ObserveAttribute_ObservesEventsWithHandler_Context()
        {
            _router.PublishEvent(new EventsForHandlerWith_Context_Evt());
            _stubEventProcessor.EventsWith_Context.Count.ShouldBe(1);
        }

        [Test]
        public void ObserveAttribute_ObservesEventsWithHandler_NoArgs()
        {
            _router.PublishEvent(new EventsForHandlerWith_NoArgs_Evt());
            _stubEventProcessor.EventsWith_NoArgs.Count.ShouldBe(1);
        }

        [Test]
        public void ObserveAttributeThrowsWhenMethodSignatureHasIncorrectParamaterCount()
        {
            var processor = new StubEventProcessorWithIncorrectSignatures_IncorrectParamaterCount(_router);
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => { processor.ObserveEvents(); });
            exception.Message.ShouldContain("Incorrect ObserveEventAttribute usage");
        }

        [Test]
        public void ObserveAttributeThrowsWhenMethodSignatureHasIncorrectParamaterTypes()
        {
            var processor1 = new StubEventProcessorWithIncorrectSignatures_IncorrectParamaterTypes1(_router);
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => { processor1.ObserveEvents(); });
            exception.Message.ShouldContain("Incorrect ObserveEventAttribute usage");

            var processor2 = new StubEventProcessorWithIncorrectSignatures_IncorrectParamaterTypes2(_router);
            exception = Assert.Throws<InvalidOperationException>(() => { processor2.ObserveEvents(); });
            exception.Message.ShouldContain("Incorrect ObserveEventAttribute usage");
        }

        [Test]
        public void ObserveAttributeObservesEventsAtCorrectStage()
        {
            _router.PublishEvent(new WorkflowTestEvent());
            _stubEventProcessor.WorkflowTestEvents.Count.ShouldBe(3);
            _stubEventProcessor.WorkflowTestEvents[0].Item3.ShouldBe(ObservationStage.Preview);
            _stubEventProcessor.WorkflowTestEvents[1].Item3.ShouldBe(ObservationStage.Normal);
            _stubEventProcessor.WorkflowTestEvents[2].Item3.ShouldBe(ObservationStage.Committed);
        }

        [Test]
        public void ObserveBaseEventFoo()
        {
            _router.PublishEvent(new FooEvent());
            _router.PublishEvent(new BarEvent());
            _router.PublishEvent(new BazEvent());
            _stubEventProcessor.BaseEvents.Count.ShouldBe(3);
            _stubEventProcessor.BaseEvents[0].Item2.GetType().ShouldBe(typeof(FooEvent));
            _stubEventProcessor.BaseEvents[0].Item3.ShouldBe(ObservationStage.Preview);
            _stubEventProcessor.BaseEvents[1].Item2.GetType().ShouldBe(typeof(BarEvent));
            _stubEventProcessor.BaseEvents[1].Item3.ShouldBe(ObservationStage.Normal);
            _stubEventProcessor.BaseEvents[2].Item2.GetType().ShouldBe(typeof(BazEvent));
            _stubEventProcessor.BaseEvents[2].Item3.ShouldBe(ObservationStage.Committed);
        }

        public class TestModel { }

        public class FooEvent : BaseEvent { }

        public class BarEvent : BaseEvent { }

        public class BazEvent : BaseEvent { }

        public class WorkflowTestEvent { }

        public class BaseEvent { }

        public class EventsForHandlerWith_Model_Event_Context_Evt { }

        public class EventsForHandlerWith_Model_Event_Evt { }

        public class EventsForHandlerWith_Model_Evt { }

        public class EventsForHandlerWith_Event_Evt { }

        public class EventsForHandlerWith_Context_Evt { }

        public class EventsForHandlerWith_NoArgs_Evt { }

        public class StubEventProcessor 
        {
            public StubEventProcessor(IRouter<TestModel> router)
            {
                FooEvents = new List<Tuple<TestModel, FooEvent, IEventContext>>();
                BarEvents = new List<Tuple<TestModel, BarEvent, IEventContext>>();
                EventsWith_Model_Event_Context = new List<Tuple<TestModel, EventsForHandlerWith_Model_Event_Context_Evt>>();
                EventsWith_Model_Event = new List<Tuple<TestModel, EventsForHandlerWith_Model_Event_Evt>>();
                EventsWith_Model = new List<TestModel>();
                EventsWith_Event = new List<EventsForHandlerWith_Event_Evt>();
                EventsWith_Context = new List<IEventContext>();
                EventsWith_NoArgs = new List<object>();
                WorkflowTestEvents = new List<Tuple<TestModel, WorkflowTestEvent, ObservationStage>>();
                BaseEvents = new List<Tuple<TestModel, BaseEvent, ObservationStage>>();

                router.ObserveEventsOn(this);
            }

            public List<Tuple<TestModel, FooEvent, IEventContext>> FooEvents { get; private set; }
            public List<Tuple<TestModel, BarEvent, IEventContext>> BarEvents { get; private set; }
            public List<Tuple<TestModel, EventsForHandlerWith_Model_Event_Context_Evt>> EventsWith_Model_Event_Context { get; private set; }
            public List<Tuple<TestModel, EventsForHandlerWith_Model_Event_Evt>> EventsWith_Model_Event { get; private set; }
            public List<TestModel> EventsWith_Model { get; private set; }
            public List<EventsForHandlerWith_Event_Evt> EventsWith_Event { get; private set; }
            public List<IEventContext> EventsWith_Context { get; private set; }
            public List<object> EventsWith_NoArgs { get; private set; }
            public List<Tuple<TestModel, WorkflowTestEvent, ObservationStage>> WorkflowTestEvents { get; private set; }
            public List<Tuple<TestModel, BaseEvent, ObservationStage>> BaseEvents { get; private set; }

            [ObserveEvent(typeof(FooEvent))]
            public void ObserveFooEvent(TestModel model, FooEvent e, IEventContext context)
            {
                FooEvents.Add(Tuple.Create(model, e, context));
            }

            [ObserveEvent(typeof(BarEvent), ObservationStage.Preview)]
            public void ObserveBarEvent(TestModel model, BarEvent e, IEventContext context)
            {
                BarEvents.Add(Tuple.Create(model, e, context));
            }

            [ObserveEvent(typeof(EventsForHandlerWith_Model_Event_Context_Evt))]
            public void ObserveObservesEventsWith_Model_Event_Context_Evt(TestModel model, EventsForHandlerWith_Model_Event_Context_Evt e, IEventContext context)
            {
                EventsWith_Model_Event_Context.Add(Tuple.Create(model, e));
            }

            [ObserveEvent(typeof(EventsForHandlerWith_Model_Event_Evt))]
            public void ObserveObservesEventsWith_Model_Event_Evt(TestModel model, EventsForHandlerWith_Model_Event_Evt e)
            {
                EventsWith_Model_Event.Add(Tuple.Create(model, e));
            }

            [ObserveEvent(typeof(EventsForHandlerWith_Model_Evt))]
            public void ObserveObservesEventsWith_Model_Evt(TestModel model)
            {
                EventsWith_Model.Add(model);
            }

            [ObserveEvent(typeof(EventsForHandlerWith_Event_Evt))]
            public void ObserveObservesEventsWith_Event_Evt(EventsForHandlerWith_Event_Evt e)
            {
                EventsWith_Event.Add(e);
            }

            [ObserveEvent(typeof(EventsForHandlerWith_Context_Evt))]
            public void ObserveObservesEventsWith_Event_Evt(IEventContext context)
            {
                EventsWith_Context.Add(context);
            }

            [ObserveEvent(typeof(EventsForHandlerWith_NoArgs_Evt))]
            public void ObserveObservesEventsWith_NoArgs_Evt()
            {
                EventsWith_NoArgs.Add(new object());
            }

            [ObserveEvent(typeof(WorkflowTestEvent), ObservationStage.Preview)]
            private void ObservePreviewTestEventAtPreview(TestModel model, WorkflowTestEvent e, IEventContext context)
            {
                WorkflowTestEvents.Add(Tuple.Create(model, e, ObservationStage.Preview));
            }

            [ObserveEvent(typeof(WorkflowTestEvent))]
            public void ObservePreviewTestEventAtNormal(TestModel model, WorkflowTestEvent e, IEventContext context)
            {
                WorkflowTestEvents.Add(Tuple.Create(model, e, ObservationStage.Normal));
                context.Commit();
            }

            [ObserveEvent(typeof(WorkflowTestEvent), ObservationStage.Committed)]
            public void ObservePreviewTestEventAtCommitted(TestModel model, WorkflowTestEvent e)
            {
                WorkflowTestEvents.Add(Tuple.Create(model, e, ObservationStage.Committed));
            }

            [ObserveBaseEvent(typeof(FooEvent), typeof(BaseEvent), ObservationStage.Preview)]
            [ObserveBaseEvent(typeof(BarEvent), typeof(BaseEvent))]
            [ObserveBaseEvent(typeof(BazEvent), typeof(BaseEvent), ObservationStage.Committed)]
            private void ObserveBaseEvent(TestModel model, BaseEvent e, IEventContext context)
            {
                BaseEvents.Add(Tuple.Create(model, e, context.CurrentStage));
            }

            [ObserveEvent(typeof(BazEvent))]
            public void ObserveBaseEventHelper(TestModel model, BazEvent e, IEventContext context)
            {
                context.Commit();
            }



            public void NothingOnThis()
            {
            }
        }

        // ReSharper disable InconsistentNaming
        public class StubEventProcessorWithIncorrectSignatures_IncorrectParamaterCount 
        {
            private readonly IRouter<TestModel> _router;

            public StubEventProcessorWithIncorrectSignatures_IncorrectParamaterCount(IRouter<TestModel> router)
            {
                _router = router;
            }

            [ObserveEvent(typeof(FooEvent))]
            public void ObserveFooEvent()
            {
            }

            public void ObserveEvents()
            {
                _router.ObserveEventsOn(this);
            }
        }

        public class StubEventProcessorWithIncorrectSignatures_IncorrectParamaterTypes1 
        {
            private readonly IRouter<TestModel> _router;

            public StubEventProcessorWithIncorrectSignatures_IncorrectParamaterTypes1(IRouter<TestModel> router)
            {
                _router = router;
            }

            [ObserveEvent(typeof(FooEvent))]
            public void ObserveFooEvent(TestModel model, int somethingWrong)
            {
            }

            public void ObserveEvents()
            {
                _router.ObserveEventsOn(this);
            }
        }

        public class StubEventProcessorWithIncorrectSignatures_IncorrectParamaterTypes2
        {
            private readonly IRouter<TestModel> _router;

            public StubEventProcessorWithIncorrectSignatures_IncorrectParamaterTypes2(IRouter<TestModel> router)
            {
                _router = router;
            }

            [ObserveEvent(typeof(FooEvent))]
            public void ObserveFooEvent(TestModel model, FooEvent e, string somethinElse)
            {
            }

            public void ObserveEvents()
            {
                _router.ObserveEventsOn(this);
            }
        }
        // ReSharper restore InconsistentNaming
    }
}
#endif