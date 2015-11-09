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

            [Test]
            public void CanObserveUsingWithModel()
            {
                new CanObserveUsingWithModel().Run();
            }

            [Test]
            public void CanObserveUsingWithEvent()
            {
                new CanObserveUsingWithEvent().Run();
            }

            [Test]
            public void CanObserveUsingWithContext()
            {
                new CanObserveUsingWithContext().Run();
            }

            [Test]
            public void CanObserveUsingWithModelAndEvent()
            {
                new CanObserveUsingWithModelAndEvent().Run();
            }

            [Test]
            public void CanObserveUsingWithEventAndContext()
            {
                new CanObserveUsingWithEventAndContext().Run();
            }

            [Test]
            public void CanObserveUsingWithModelAndEventAndContext()
            {
                new CanObserveUsingWithModelAndEventAndContext().Run();
            }

            [Test]
            public void CanObserveUsingWithModelAndEventAndContextOrderUnimportant()
            {
                new CanObserveUsingWithModelAndEventAndContextOrderUnimportant().Run();
            }

            [Test]
            public void CanObserveWithLessSpeificEventtype()
            {
                new CanObserveWithLessSpeificEventtype().Run();
            }

            [Test]
            public void ObserveThrowsWhenThereAreAdditionalMethodParams()
            {
                new ObserveThrowsWhenThereAreAdditionalMethodParams().Run();
            }

            [Test]
            public void ObserveThrowsWhenModelOfIncorrectType()
            {
                new ObserveThrowsWhenModelOfIncorrectType().Run();
            }

            [Test]
            public void ObserveThrowsWhenThereAreDuplicateEvents()
            {
                new ObserveThrowsWhenThereAreDuplicateEvents().Run();
            }

            [Test]
            public void ObserveThrowsWhenMultipleEventsDontShareSameBaseEvent_1()
            {
                new ObserveThrowsWhenMultipleEventsDontShareSameBaseEvent_1().Run();
            }

            [Test]
            public void ObserveThrowsWhenMultipleEventsDontShareSameBaseEvent_2()
            {
                new ObserveThrowsWhenMultipleEventsDontShareSameBaseEvent_2().Run();
            }

            [Test]
            public void CanObserveMultipleEventsByBaseEventType()
            {
                new CanObserveMultipleEventsByBaseEventType().Run();
            }

            [Test]
            public void CanObserveMultipleEventsByBaseEventTypeAtCorrectStaage()
            {
                new CanObserveMultipleEventsByBaseEventTypeAtCorrectStaage().Run();
            }
        }

        public class CanObserveUsingObserveAttribute : ObserveEventsOnBase
        {
            private bool _received;

            protected override void RunTest()
            {
                ObserveEventsOnThis();
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
                ObserveEventsOnThis();
                Router.PublishEvent(new FooEvent());
                _received.ShouldBe(true);
            }

            [ObserveEvent(typeof(FooEvent))]
            public void ObserveFooEvent()
            {
                _received = true;
            }
        }


        public class CanObserveUsingWithModel : ObserveEventsOnBase
        {
            public TestModel ReceivedModel;

            protected override void RunTest()
            {
                ObserveEventsOnThis();
                Router.PublishEvent(new FooEvent());
                ReceivedModel.ShouldBe(Model);
            }

            [ObserveEvent(typeof(FooEvent))]
            public void ObserveFooEvent(TestModel model)
            {
                ReceivedModel = model;
            }
        }

        public class CanObserveUsingWithEvent : ObserveEventsOnBase
        {
            public FooEvent ReceivedEvent;

            protected override void RunTest()
            {
                ObserveEventsOnThis();
                var fooEvent = new FooEvent();
                Router.PublishEvent(fooEvent);
                ReceivedEvent.ShouldBe(fooEvent);
            }

            [ObserveEvent(typeof(FooEvent))]
            public void ObserveFooEvent(FooEvent @event)
            {
                ReceivedEvent = @event;
            }
        }

        public class CanObserveUsingWithContext : ObserveEventsOnBase
        {
            public bool ContextWasReceived;

            protected override void RunTest()
            {
                ObserveEventsOnThis();
                var fooEvent = new FooEvent();
                Router.PublishEvent(fooEvent);
                ContextWasReceived.ShouldBe(true);
            }

            [ObserveEvent(typeof(FooEvent))]
            public void ObserveFooEvent(IEventContext context)
            {
                ContextWasReceived = context != null;
            }
        }

        public class CanObserveUsingWithModelAndEvent : ObserveEventsOnBase
        {
            public FooEvent ReceivedEvent;
            public TestModel ReceivedModel;

            protected override void RunTest()
            {
                ObserveEventsOnThis();
                var fooEvent = new FooEvent();
                Router.PublishEvent(fooEvent);
                ReceivedEvent.ShouldBe(fooEvent);
                ReceivedModel.ShouldBe(Model);
            }

            [ObserveEvent(typeof(FooEvent))]
            public void ObserveFooEvent(TestModel model, FooEvent e)
            {
                ReceivedModel = model;
                ReceivedEvent = e;
            }
        }

        public class CanObserveUsingWithEventAndContext : ObserveEventsOnBase
        {
            public FooEvent ReceivedEvent;
            public bool ContextWasReceived;

            protected override void RunTest()
            {
                ObserveEventsOnThis();
                var fooEvent = new FooEvent();
                Router.PublishEvent(fooEvent);
                ReceivedEvent.ShouldBe(fooEvent);
                ContextWasReceived.ShouldBe(true);
            }

            [ObserveEvent(typeof(FooEvent))]
            public void ObserveFooEvent(FooEvent e, IEventContext context)
            {
                ReceivedEvent = e;
                ContextWasReceived = context != null;
            }
        }

        public class CanObserveUsingWithModelAndEventAndContext : ObserveEventsOnBase
        {
            public TestModel ReceivedModel;
            public FooEvent ReceivedEvent;
            public bool ContextWasReceived;

            protected override void RunTest()
            {
                ObserveEventsOnThis();
                var fooEvent = new FooEvent();
                Router.PublishEvent(fooEvent);
                ReceivedEvent.ShouldBe(fooEvent);
                ContextWasReceived.ShouldBe(true);
                ReceivedModel.ShouldBe(Model);
            }

            [ObserveEvent(typeof(FooEvent))]
            public void ObserveFooEvent(TestModel model, FooEvent e, IEventContext context)
            {
                ReceivedModel = model;
                ReceivedEvent = e;
                ContextWasReceived = context != null;
            }
        }

        public class CanObserveUsingWithModelAndEventAndContextOrderUnimportant : ObserveEventsOnBase
        {
            public TestModel ReceivedModel;
            public FooEvent ReceivedEvent;
            public bool ContextWasReceived;

            protected override void RunTest()
            {
                ObserveEventsOnThis();
                var fooEvent = new FooEvent();
                Router.PublishEvent(fooEvent);
                ReceivedEvent.ShouldBe(fooEvent);
                ContextWasReceived.ShouldBe(true);
                ReceivedModel.ShouldBe(Model);
            }

            [ObserveEvent(typeof(FooEvent))]
            public void ObserveFooEvent(IEventContext context, FooEvent e, TestModel model)
            {
                ReceivedModel = model;
                ReceivedEvent = e;
                ContextWasReceived = context != null;
            }
        }

        public class CanObserveWithLessSpeificEventtype : ObserveEventsOnBase
        {
            public BaseEvent ReceivedEvent;

            protected override void RunTest()
            {
                ObserveEventsOnThis();
                var fooEvent = new FooEvent();
                Router.PublishEvent(fooEvent);
                ReceivedEvent.ShouldBe(fooEvent);
            }

            [ObserveEvent(typeof(FooEvent))]
            public void CommitBuzz(/* note we use the base event type here */ BaseEvent e, IEventContext context, TestModel model)
            {
                ReceivedEvent = e;
            }
        }

        public class ObserveThrowsWhenThereAreAdditionalMethodParams : ObserveEventsOnBase
        { 
            protected override void RunTest()
            {
                InvalidOperationException ex = Assert.Throws<InvalidOperationException>(ObserveEventsOnThis);
                ex.Message.ShouldContain("Incorrect ObserveEventAttribute usage on method");
            }

            [ObserveEvent(typeof(FooEvent))]
            public void ObserveFooEvent(TestModel model, FooEvent e, IEventContext context, string foo)
            {
            }
        }

        public class ObserveThrowsWhenModelOfIncorrectType : ObserveEventsOnBase
        {
            protected override void RunTest()
            {
                InvalidOperationException ex = Assert.Throws<InvalidOperationException>(ObserveEventsOnThis);
                ex.Message.ShouldContain("Incorrect ObserveEventAttribute usage on method");
            }

            [ObserveEvent(typeof(FooEvent))]
            public void ObserveFooEvent(object model, FooEvent e, IEventContext context)
            {
            }
        }

        public class ObserveThrowsWhenThereAreDuplicateEvents : ObserveEventsOnBase
        {
            protected override void RunTest()
            {
                InvalidOperationException ex = Assert.Throws<InvalidOperationException>(ObserveEventsOnThis);
                ex.Message.ShouldContain("Incorrect ObserveEventAttribute usage on method");
            }

            [ObserveEvent(typeof(FooEvent))]
            public void ObserveFooEvent(FooEvent e1, FooEvent e)
            {
            }
        }

        public class ObserveThrowsWhenMultipleEventsDontShareSameBaseEvent_1 : ObserveEventsOnBase
        {
            protected override void RunTest()
            {
                InvalidOperationException ex = Assert.Throws<InvalidOperationException>(ObserveEventsOnThis);
                ex.Message.ShouldContain("Could not determine a common base event type");
            }

            // here the code has to infer the events from the attributes AND ensure the BaseEvent in the method is correct
            [ObserveEvent(typeof(FooEvent))]
            [ObserveEvent(typeof(StandaloneEvent))] // not a BaseEvent
            public void ObserveFooEvent(BaseEvent e1, IEventContext context, TestModel model)
            {
            }
        }

        public class ObserveThrowsWhenMultipleEventsDontShareSameBaseEvent_2 : ObserveEventsOnBase
        {
            protected override void RunTest()
            {
                InvalidOperationException ex = Assert.Throws<InvalidOperationException>(ObserveEventsOnThis);
                ex.Message.ShouldContain("Could not determine a common base event type");
            }

            // here the code has to infer the events from the attributes alone
            [ObserveEvent(typeof(FooEvent))]
            [ObserveEvent(typeof(StandaloneEvent))] 
            public void ObserveFooEvent() 
            {
            }
        }

        public class CanObserveMultipleEventsByBaseEventType : ObserveEventsOnBase
        {
            private List<BaseEvent> _receivedEvents = new List<BaseEvent>();

            protected override void RunTest()
            {
                ObserveEventsOnThis();
                var fooEvent = new FooEvent();
                var barEvent = new BarEvent();
                Router.PublishEvent(fooEvent);
                Router.PublishEvent(barEvent);
                _receivedEvents.Count.ShouldBe(2);
                _receivedEvents[0].ShouldBe(fooEvent);
                _receivedEvents[1].ShouldBe(barEvent);
            }

            [ObserveEvent(typeof(FooEvent))]
            [ObserveEvent(typeof(BarEvent))]
            public void ObserveByBaseEvent(BaseEvent e, IEventContext context, TestModel model)
            {
                _receivedEvents.Add(e);
            }
        }

        public class CanObserveMultipleEventsByBaseEventTypeAtCorrectStaage : ObserveEventsOnBase
        {
            private List<Tuple<BaseEvent, ObservationStage>> _receivedEvents = new List<Tuple<BaseEvent, ObservationStage>>();

            protected override void RunTest()
            {
                ObserveEventsOnThis();
                var fooEvent = new FooEvent();
                var barEvent = new BarEvent();
                var bazEvent = new BazEvent();
                var buzzEvent = new BuzzEvent();

                Router.PublishEvent(fooEvent);
                AssertLastReceivedEvent(1, ObservationStage.Preview, fooEvent);

                Router.PublishEvent(barEvent);
                AssertLastReceivedEvent(2, ObservationStage.Normal, barEvent);

                Router.PublishEvent(bazEvent);
                AssertLastReceivedEvent(3, ObservationStage.Normal, bazEvent);

                Router.PublishEvent(buzzEvent);
                AssertLastReceivedEvent(4, ObservationStage.Committed, buzzEvent);
            }

            [ObserveEvent(typeof(BuzzEvent))]
            public void CommitBuzz(BuzzEvent e, IEventContext context, TestModel model)
            {
                context.Commit();
            }

            [ObserveEvent(typeof(FooEvent), ObservationStage.Preview)]
            [ObserveEvent(typeof(BarEvent))]
            [ObserveEvent(typeof(BazEvent), ObservationStage.Normal)]
            [ObserveEvent(typeof(BuzzEvent), ObservationStage.Committed)]
            public void ObserveByBaseEvent(BaseEvent e, IEventContext context, TestModel model)
            {
                _receivedEvents.Add(Tuple.Create(e, context.CurrentStage));
            }

            private void AssertLastReceivedEvent(int expectedEventReceivedCount, ObservationStage expectedObservationStage, BaseEvent sent)
            {
                _receivedEvents.Count.ShouldBe(expectedEventReceivedCount);
                _receivedEvents[expectedEventReceivedCount -1].Item2.ShouldBe(expectedObservationStage);
                _receivedEvents[expectedEventReceivedCount - 1].Item1.ShouldBe(sent);
            }
        }

        // Because the event wire up is done for all method in one shot (when you call ObserveEventsOn(obj)) it makes sense to have each test in it's own class. 
        // This helps find issues as each test only deals with attributed method at a time. 
        // Without this setup, with all the attributed methods in one class, you have a lot of event wire-up logic running  for every tests, makes it hard to see where it issues are.
        public abstract class ObserveEventsOnBase
        {
            protected Router<TestModel> Router;
            protected TestModel Model;

            public void SetUp()
            {
                Router = new Router<TestModel>(new StubRouterDispatcher());
                Model = new TestModel();
                Router.SetModel(Model);
            }

            public void Run()
            {
                SetUp();
                RunTest();
            }

            protected abstract void RunTest();

            protected void ObserveEventsOnThis()
            {
                Router.ObserveEventsOn(this);
            }

            public class TestModel { }
            public class FooEvent : BaseEvent { }
            public class BarEvent : BaseEvent { }
            public class BazEvent : BaseEvent { }
            public class BuzzEvent : BaseEvent { }
            public class StandaloneEvent { }
        }
    }
}
#endif