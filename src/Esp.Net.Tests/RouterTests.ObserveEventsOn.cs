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
    public partial class RouterTests
    {
        [TestFixture]
        public class ObserveEventsOnTests
        {
            [Test]
            public void CanObserveUsingObserveAttribute()
            {
                new CanObserveUsingObserveAttribute().Run();
            }

            [Test]
            public void CanObserveUsingWithNoParams()
            {
                new CanObserveUsingWithNoParams().Run();
            }
        }

        public class CanObserveUsingObserveAttribute : ObserveEventsOnBase
        {
            private bool _received;

            protected override void RunTest()
            {
                Router.PublishEvent(new FooEvent());
                _received.ShouldBe(true);
            }

            [ObserveEvent(typeof(FooEvent))]
            public void ObserveFooEvent(FooEvent e, IEventContext context, TestModel model)
            {
                _received = true;
            }
        }

        public class CanObserveUsingWithNoParams : ObserveEventsOnBase
        {
            private bool _received;

            protected override void RunTest()
            {
                Router.PublishEvent(new FooEvent());
                _received.ShouldBe(true);
            }

            [ObserveEvent(typeof(FooEvent))]
            public void ObserveFooEvent()
            {
                _received = true;
            }
        }

        // Because the event wire up is done for all method in one shot (when you call ObserveEventsOn(obj)) it makes sense to have each test in it's own class. 
        // This helps find issues as each test only deals with attributed method at a time. 
        // Without this setup, with all the attributed methods in one class, you have a lot of event wire-up logic running  for every tests, makes it hard to see where it issues are.
        public abstract class ObserveEventsOnBase
        {
            protected Router<TestModel> Router;

            public void SetUp()
            {
                Router = new Router<TestModel>(new StubRouterDispatcher());
                Router.SetModel(new TestModel());
                Router.ObserveEventsOn(this);
            }

            public void Run()
            {
                SetUp();
                RunTest();
            }

            protected abstract void RunTest();

            public class TestModel { }

            public class FooEvent : BaseEvent { }

            public class BarEvent : BaseEvent { }
        }
    }
//
//        [TestFixture]
//        public class ObserveEventsOnTests
//        {
//            private IRouter<TestModel> _router;
//            private StubEventProcessor _stubEventProcessor;
//
//            [SetUp]
//            public void SetUp()
//            {
//                var router = new Router(new StubRouterDispatcher());
//                var modelId = Guid.NewGuid();
//                router.AddModel(modelId, new TestModel());
//                _router = new Router<TestModel>(modelId, router);
//                _stubEventProcessor = new StubEventProcessor(_router);
//            }
//
//            [Test]
//            public void ObserveAttribute_ObservesEventsWithHandler_Event_Context_Model()
//            {
//                _router.PublishEvent(new EventsForHandlerWith_Event_Context_Model_Evt());
//                _stubEventProcessor.EventsWith_Model_Event_Context.ShouldBe(1);
//            }
//
//            [Test]
//            public void ObserveAttribute_ObservesEventsWithHandler_Event_Model()
//            {
//                _router.PublishEvent(new EventsForHandlerWith_Event_Model_Evt());
//                _stubEventProcessor.EventsWith_Model_Event.ShouldBe(1);
//            }
//
//            [Test]
//            public void ObserveAttribute_ObservesEventsWithHandler_Model()
//            {
//                _router.PublishEvent(new EventsForHandlerWith_Model_Evt());
//                _stubEventProcessor.EventsWith_Model.ShouldBe(1);
//            }
//
//            [Test]
//            public void ObserveAttribute_ObservesEventsWithHandler_Event_Context()
//            {
//                _router.PublishEvent(new EventsWith_Event_Context_Evt());
//                _stubEventProcessor.EventsWith_Event_Context.ShouldBe(1);
//            }
//
//            [Test]
//            public void ObserveAttribute_ObservesEventsWithHandler_Event()
//            {
//                _router.PublishEvent(new EventsForHandlerWith_Event_Evt());
//                _stubEventProcessor.EventsWith_Event.ShouldBe(1);
//            }
//
//            [Test]
//            public void ObserveAttribute_ObservesEventsWithHandler_Context()
//            {
//                _router.PublishEvent(new EventsForHandlerWith_Context_Evt());
//                _stubEventProcessor.EventsWith_Context.ShouldBe(1);
//            }
//
//            [Test]
//            public void ObserveAttribute_ObservesEventsWithHandler_NoArgs()
//            {
//                _router.PublishEvent(new EventsForHandlerWith_NoArgs_Evt());
//                _stubEventProcessor.EventsWith_NoArgs.ShouldBe(1);
//            }
//
//            [Test]
//            public void ObserveAttributeThrowsWhenMethodSignatureHasIncorrectParamaterCount()
//            {
//                var processor = new StubEventProcessorWithIncorrectSignatures_IncorrectParamaterCount(_router);
//                InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => { processor.ObserveEvents(); });
//                exception.Message.ShouldContain("Incorrect ObserveEventAttribute usage");
//            }
//
//            [Test]
//            public void ObserveAttributeThrowsWhenMethodSignatureHasIncorrectParamaterTypes()
//            {
//                var processor1 = new StubEventProcessorWithIncorrectSignatures_IncorrectParamaterTypes1(_router);
//                InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => { processor1.ObserveEvents(); });
//                exception.Message.ShouldContain("Incorrect ObserveEventAttribute usage");
//
//                var processor2 = new StubEventProcessorWithIncorrectSignatures_IncorrectParamaterTypes2(_router);
//                exception = Assert.Throws<InvalidOperationException>(() => { processor2.ObserveEvents(); });
//                exception.Message.ShouldContain("Incorrect ObserveEventAttribute usage");
//            }
//
//            [Test]
//            public void ObserveAttributeObservesEventsAtCorrectStage()
//            {
//                _router.PublishEvent(new WorkflowTestEvent());
//                _stubEventProcessor.WorkflowTestEvents.Count.ShouldBe(3);
//                _stubEventProcessor.WorkflowTestEvents[0].Item2.ShouldBe(ObservationStage.Preview);
//                _stubEventProcessor.WorkflowTestEvents[1].Item2.ShouldBe(ObservationStage.Normal);
//                _stubEventProcessor.WorkflowTestEvents[2].Item2.ShouldBe(ObservationStage.Committed);
//            }
//
//            [Test]
//            public void ObserveBaseEventFoo()
//            {
//                _router.PublishEvent(new FooEvent());
//                _router.PublishEvent(new BarEvent());
//                _router.PublishEvent(new BazEvent());
//                _stubEventProcessor.BaseEvents.Count.ShouldBe(3);
////                _stubEventProcessor.BaseEvents[0].Item1.GetType().ShouldBe(typeof(FooEvent));
////                _stubEventProcessor.BaseEvents[0].Item2.ShouldBe(ObservationStage.Preview);
////                _stubEventProcessor.BaseEvents[1].Item1.GetType().ShouldBe(typeof(BarEvent));
////                _stubEventProcessor.BaseEvents[1].Item2.ShouldBe(ObservationStage.Normal);
////                _stubEventProcessor.BaseEvents[2].Item1.GetType().ShouldBe(typeof(BazEvent));
////                _stubEventProcessor.BaseEvents[2].Item2.ShouldBe(ObservationStage.Committed);
//            }
//
//            public class TestModel { }
//
//            public class FooEvent : BaseEvent { }
//
//            public class BarEvent : BaseEvent { }
//
//            public class BazEvent : BaseEvent { }
//
//            public class WorkflowTestEvent { }
//
//            public class BaseEvent { }
//
//            public class EventsForHandlerWith_Event_Context_Model_Evt { }
//
//            public class EventsForHandlerWith_Event_Model_Evt { }
//
//            public class EventsForHandlerWith_Model_Evt { }
//
//            public class EventsForHandlerWith_Event_Evt { }
//
//            public class EventsForHandlerWith_Context_Evt { }
//
//            public class EventsForHandlerWith_NoArgs_Evt { }
//
//            public class EventsWith_Event_Context_Evt { }
//
//            public class StubEventProcessor
//            {
//                public StubEventProcessor(IRouter<TestModel> router)
//                {
//                    FooEvents = new List<Tuple<FooEvent, IEventContext, TestModel>>();
//                    BarEvents = new List<Tuple<BarEvent, IEventContext, TestModel>>();
//                    WorkflowTestEvents = new List<Tuple<WorkflowTestEvent, ObservationStage, TestModel>>();
//                    BaseEvents = new List<Tuple<BaseEvent, ObservationStage, TestModel>>();
//                    router.ObserveEventsOn(this);
//                }
//
//                public List<Tuple<FooEvent, IEventContext, TestModel>> FooEvents { get; private set; }
//                public List<Tuple<BarEvent, IEventContext, TestModel>> BarEvents { get; private set; }
//                public int EventsWith_Model_Event_Context { get; private set; }
//                public int EventsWith_Model_Event { get; private set; }
//                public int EventsWith_Model { get; private set; }
//                public int EventsWith_Event { get; private set; }
//                public int EventsWith_Context { get; private set; }
//                public int EventsWith_NoArgs { get; private set; }
//                public int EventsWith_Event_Context { get; private set; }
//                public List<Tuple<WorkflowTestEvent, ObservationStage, TestModel>> WorkflowTestEvents { get; private set; }
//                public List<Tuple<BaseEvent, ObservationStage, TestModel>> BaseEvents { get; private set; }
//
//                [ObserveEvent(typeof(FooEvent))]
//                public void ObserveFooEvent(FooEvent e, IEventContext context, TestModel model)
//                {
//                    FooEvents.Add(Tuple.Create(e, context, model));
//                }
//
//                [ObserveEvent(typeof(BarEvent), ObservationStage.Preview)]
//                public void ObserveBarEvent(BarEvent e, IEventContext context, TestModel model)
//                {
//                    BarEvents.Add(Tuple.Create(e, context, model));
//                }
//
//                [ObserveEvent(typeof(EventsForHandlerWith_Event_Context_Model_Evt))]
//                public void ObserveObservesEventsWith_Model_Event_Context_Evt(EventsForHandlerWith_Event_Context_Model_Evt e, IEventContext context, TestModel model)
//                {
//                    EventsWith_Model_Event_Context++;
//                }
//
//                [ObserveEvent(typeof(EventsForHandlerWith_Event_Model_Evt))]
//                public void ObserveObservesEventsWith_Model_Event_Evt(EventsForHandlerWith_Event_Model_Evt e, TestModel model)
//                {
//                    EventsWith_Model_Event++;
//                }
//
//                [ObserveEvent(typeof(EventsForHandlerWith_Model_Evt))]
//                public void ObserveObservesEventsWith_Model_Evt(TestModel model)
//                {
//                    EventsWith_Model++;
//                }
//
//                [ObserveEvent(typeof(EventsWith_Event_Context_Evt))]
//                public void ObserveObservesEventsWith_Event__Context_Evt(EventsWith_Event_Context_Evt e, IEventContext context)
//                {
//                    EventsWith_Event_Context++;
//                }
//
//                [ObserveEvent(typeof(EventsForHandlerWith_Event_Evt))]
//                public void ObserveObservesEventsWith_Event_Evt(EventsForHandlerWith_Event_Evt e)
//                {
//                    EventsWith_Event++;
//                }
//
//                [ObserveEvent(typeof(EventsForHandlerWith_Context_Evt))]
//                public void ObserveObservesEventsWith_Event_Evt(IEventContext context)
//                {
//                    EventsWith_Context++;
//                }
//
//                [ObserveEvent(typeof(EventsForHandlerWith_NoArgs_Evt))]
//                public void ObserveObservesEventsWith_NoArgs_Evt()
//                {
//                    EventsWith_NoArgs++;
//                }
//
//                [ObserveEvent(typeof(WorkflowTestEvent), ObservationStage.Preview)]
//                private void ObservePreviewTestEventAtPreview(WorkflowTestEvent e, IEventContext context, TestModel model)
//                {
//                    WorkflowTestEvents.Add(Tuple.Create(e, ObservationStage.Preview, model));
//                }
//
//                [ObserveEvent(typeof(WorkflowTestEvent))]
//                public void ObservePreviewTestEventAtNormal(WorkflowTestEvent e, IEventContext context, TestModel model)
//                {
//                    WorkflowTestEvents.Add(Tuple.Create(e, ObservationStage.Normal, model));
//                    context.Commit();
//                }
//
//                [ObserveEvent(typeof(WorkflowTestEvent), ObservationStage.Committed)]
//                public void ObservePreviewTestEventAtCommitted(WorkflowTestEvent e, TestModel model)
//                {
//                    WorkflowTestEvents.Add(Tuple.Create(e, ObservationStage.Committed, model));
//                }
//
////                [ObserveBaseEvent(typeof(FooEvent), typeof(BaseEvent), ObservationStage.Preview)]
////                [ObserveBaseEvent(typeof(BarEvent), typeof(BaseEvent))]
////                [ObserveBaseEvent(typeof(BazEvent), typeof(BaseEvent), ObservationStage.Committed)]
//
//                [ObserveEvent(typeof(FooEvent), ObservationStage.Preview)]
//                [ObserveEvent(typeof(BarEvent))]
//                [ObserveEvent(typeof(BazEvent), ObservationStage.Committed)]
//                private void ObserveBaseEvent(BaseEvent e, IEventContext context, TestModel model)
//                {
//                    BaseEvents.Add(Tuple.Create(e, context.CurrentStage, model));
//                }
//
//                [ObserveEvent(typeof(BazEvent))]
//                public void ObserveBaseEventHelper(BazEvent e, IEventContext context, TestModel model)
//                {
//                    context.Commit();
//                }
//
//                public void NothingOnThis()
//                {
//                }
//            }
//
//            // ReSharper disable InconsistentNaming
//            public class StubEventProcessorWithIncorrectSignatures_IncorrectParamaterCount
//            {
//                private readonly IRouter<TestModel> _router;
//
//                public StubEventProcessorWithIncorrectSignatures_IncorrectParamaterCount(IRouter<TestModel> router)
//                {
//                    _router = router;
//                }
//
//                [ObserveEvent(typeof(FooEvent))]
//                public void ObserveFooEvent(TestModel m, FooEvent e, IEventContext c, int b)
//                {
//                }
//
//                public void ObserveEvents()
//                {
//                    _router.ObserveEventsOn(this);
//                }
//            }
//
//            public class StubEventProcessorWithIncorrectSignatures_IncorrectParamaterTypes1
//            {
//                private readonly IRouter<TestModel> _router;
//
//                public StubEventProcessorWithIncorrectSignatures_IncorrectParamaterTypes1(IRouter<TestModel> router)
//                {
//                    _router = router;
//                }
//
//                [ObserveEvent(typeof(FooEvent))]
//                public void ObserveFooEvent(TestModel model, int somethingWrong)
//                {
//                }
//
//                public void ObserveEvents()
//                {
//                    _router.ObserveEventsOn(this);
//                }
//            }
//
//            public class StubEventProcessorWithIncorrectSignatures_IncorrectParamaterTypes2
//            {
//                private readonly IRouter<TestModel> _router;
//
//                public StubEventProcessorWithIncorrectSignatures_IncorrectParamaterTypes2(IRouter<TestModel> router)
//                {
//                    _router = router;
//                }
//
//                [ObserveEvent(typeof(FooEvent))]
//                public void ObserveFooEvent(TestModel model, FooEvent e, string somethinElse)
//                {
//                }
//
//                public void ObserveEvents()
//                {
//                    _router.ObserveEventsOn(this);
//                }
//            }
//            // ReSharper restore InconsistentNaming
//        }
//    }
}
#endif