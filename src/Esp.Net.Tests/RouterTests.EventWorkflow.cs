﻿using System;
using System.Collections.Generic;
using System.Linq;
using Esp.Net.Meta;
using Esp.Net.Reactive;
using NUnit.Framework;
using Shouldly;

namespace Esp.Net
{
    public partial class RouterTests
    {
        public class EventWorkflow : RouterTests
        {
            [Test]
            public void PreProcessorInvokedForFirstEvent()
            {
                PublishEventWithMultipeSubsequentEvents(2);
                _model1EventProcessor.Event1Details.NormalStage.ReceivedEvents.Count.ShouldBe(3);
                _model1PreEventProcessor.InvocationCount.ShouldBe(1);
            }

            [Test]
            public void PreviewObservationStageObserversRecievEvent()
            {
                _router.PublishEvent(_model1.Id, new Event1());
                _model1EventProcessor.Event1Details.PreviewStage.ReceivedEvents.Count.ShouldBe(1);
                _model1EventProcessor2.Event1Details.PreviewStage.ReceivedEvents.Count.ShouldBe(1);
            }

            [Test]
            public void NormalObservationStageObserversRecieveEvent()
            {
                _router.PublishEvent(_model1.Id, new Event1());
                _model1EventProcessor.Event1Details.NormalStage.ReceivedEvents.Count.ShouldBe(1);
                _model1EventProcessor2.Event1Details.NormalStage.ReceivedEvents.Count.ShouldBe(1);
            }

            [Test]
            public void CommittedObservationStageObserversRecieveEvent()
            {
                _router.PublishEvent(_model1.Id, new Event1 { ShouldCommit = true, CommitAtStage = ObservationStage.Normal, CommitAtEventProcesserId = EventProcessor1Id });
                _model1EventProcessor.Event1Details.CommittedStage.ReceivedEvents.Count.ShouldBe(1);
                _model1EventProcessor2.Event1Details.CommittedStage.ReceivedEvents.Count.ShouldBe(1);
            }

            [Test]
            public void PostProcessorInvokedAfterAllEventsPurged()
            {
                PublishEventWithMultipeSubsequentEvents(2);
                _model1EventProcessor.Event1Details.NormalStage.ReceivedEvents.Count.ShouldBe(3);
                _model1PostEventProcessor.InvocationCount.ShouldBe(1);
            }

            [Test]
            public void EventSentToPreProcessorThenEventProcessorThenPostProcessors()
            {
                var order = new List<int>();
                _model1PreEventProcessor.RegisterAction(model => order.Add(1));
                _model1EventProcessor.Event1Details.PreviewStage.RegisterAction((m, e) => order.Add(2));
                _model1EventProcessor.Event1Details.NormalStage.RegisterAction((m, e) => order.Add(3));
                _model1EventProcessor.Event1Details.CommittedStage.RegisterAction((m, e) => order.Add(4));
                _model1PostEventProcessor.RegisterAction(model => order.Add(5));
                _router.PublishEvent(_model1.Id, new Event1 { ShouldCommit = true, CommitAtStage = ObservationStage.Normal, CommitAtEventProcesserId = EventProcessor2Id });
                order.ShouldBe(new[] { 1, 2, 3, 4, 5 });
            }

            [Test]
            public void EventWorkflowOnlyInvokedForTargetModel()
            {
                _router.PublishEvent(_model1.Id, new Event1());
                _model1PreEventProcessor.InvocationCount.ShouldBe(1);
                _model2PreEventProcessor.InvocationCount.ShouldBe(0);
                _model1EventProcessor.Event1Details.NormalStage.ReceivedEvents.Count.ShouldBe(1);
                _model2EventProcessor.Event1Details.NormalStage.ReceivedEvents.Count.ShouldBe(0);
                _model1PostEventProcessor.InvocationCount.ShouldBe(1);
                _model2PostEventProcessor.InvocationCount.ShouldBe(0);

                _router.PublishEvent(_model2.Id, new Event1());
                _model1PreEventProcessor.InvocationCount.ShouldBe(1);
                _model2PreEventProcessor.InvocationCount.ShouldBe(1);
                _model1EventProcessor.Event1Details.NormalStage.ReceivedEvents.Count.ShouldBe(1);
                _model2EventProcessor.Event1Details.NormalStage.ReceivedEvents.Count.ShouldBe(1);
                _model1PostEventProcessor.InvocationCount.ShouldBe(1);
                _model2PostEventProcessor.InvocationCount.ShouldBe(1);
            }

            [Test]
            public void OnlyProcessEventsIfThereAreObservers()
            {
                _router.PublishEvent(_model1.Id, "AnEventWithNoObservers");
                _model1PreEventProcessor.InvocationCount.ShouldBe(0);
                _model1PostEventProcessor.InvocationCount.ShouldBe(0);
                _model1Controller.ReceivedModels.Count.ShouldBe(0);
            }

            [Test]
            public void PreAndPostProcessorOnModelInvokedDuringEventWorkflow()
            {
                _router.GetEventObservable<Event1, TestModel5>(_model5.Id).Observe((e, c) => { /* noop */});
                _router.PublishEvent(_model5.Id, new Event1());
                _model5.PreProcessorInvocationCount.ShouldBe(1);
                _model5.PostProcessorInvocationCount.ShouldBe(1);
                _router.PublishEvent(_model5.Id, new Event1());
                _model5.PreProcessorInvocationCount.ShouldBe(2);
                _model5.PostProcessorInvocationCount.ShouldBe(2);
            }

            public class SubsequentEvents : RouterTests
            {
                [Test]
                public void EventsPublishedByPreProcessorGetProcessedFromBackingQueue()
                {
                    _model1PreEventProcessor.RegisterAction(m => _router.PublishEvent(_model1.Id, new Event1("B")));
                    _router.PublishEvent(_model1.Id, new Event1("A"));
                    AssertReceivedEventPayloadsAreInOrder("A", "B");
                }

                [Test]
                public void EventsPublishedByPreviewObservationStageObserversGetProcessedFromBackingQueue()
                {
                    _model1EventProcessor.Event1Details.PreviewStage.RegisterAction((m, e) =>
                    {
                        if (e.Payload != "B") _router.PublishEvent(_model1.Id, new Event1("B"));
                    });
                    _router.PublishEvent(_model1.Id, new Event1("A"));
                    AssertReceivedEventPayloadsAreInOrder("A", "B");
                }

                [Test]
                public void EventsPublishedByNormalObservationStageObserversGetProcessedFromBackingQueue()
                {
                    _model1EventProcessor.Event1Details.PreviewStage.RegisterAction((m, e) =>
                    {
                        if (e.Payload != "B") _router.PublishEvent(_model1.Id, new Event1("B"));
                    });
                    _model1EventProcessor.Event1Details.CommittedStage.RegisterAction((m, e) =>
                    {
                        // at this point B should be published but not processed, 
                        // this hadler should first as we finish dispatching to handlers for the initial event 'A'
                        AssertReceivedEventPayloadsAreInOrder("A");
                    });
                    _router.PublishEvent(_model1.Id, new Event1("A") { ShouldCommit = true, CommitAtStage = ObservationStage.Normal, CommitAtEventProcesserId = EventProcessor1Id });
                    AssertReceivedEventPayloadsAreInOrder("A", "B");
                }

                [Test]
                public void EventsPublishedByCommittedObservationStageObserversGetProcessedFromBackingQueue()
                {
                    bool hasPublishedB = false;
                    var passed = false;
                    _model1EventProcessor.Event1Details.CommittedStage.RegisterAction((m, e) =>
                    {
                        if (!hasPublishedB)
                        {
                            hasPublishedB = true;
                            _router.PublishEvent(_model1.Id, new Event1("B"));
                        }
                    });
                    _model1EventProcessor2.Event1Details.CommittedStage.RegisterAction((m, e) =>
                    {
                        // We need to check that all the handlers for event A run before 
                        // event B is processed.
                        passed =
                            hasPublishedB &&
                            _model1EventProcessor.Event1Details.NormalStage.ReceivedEvents.Count == 1 &&
                            _model1EventProcessor.Event1Details.NormalStage.ReceivedEvents[0].Payload == "A" &&
                            _model1EventProcessor2.Event1Details.NormalStage.ReceivedEvents.Count == 1 &&
                            _model1EventProcessor2.Event1Details.NormalStage.ReceivedEvents[0].Payload == "A";
                    });
                    _router.PublishEvent(_model1.Id, new Event1("A") { ShouldCommit = true, CommitAtStage = ObservationStage.Normal, CommitAtEventProcesserId = EventProcessor1Id });
                    passed.ShouldBe(true);
                }

                [Test]
                public void EventsPublishedByPostProcessorGetProcessedInANewEventLoop()
                {
                    bool hasPublishedB = false;
                    _model1PostEventProcessor.RegisterAction(m =>
                    {
                        if (!hasPublishedB)
                        {
                            hasPublishedB = true;
                            _router.PublishEvent(_model1.Id, new Event1("B"));
                        }
                    });
                    _router.PublishEvent(_model1.Id, new Event1("A"));
                    // the pre processor will run again as the event workflow for this model is restarted
                    _model1PreEventProcessor.InvocationCount.ShouldBe(2);
                    // however we won't dispatch 2 udpates as we'll process all events first
                    _model1Controller.ReceivedModels.Count.ShouldBe(1);
                }

                private void AssertReceivedEventPayloadsAreInOrder(params string[] args)
                {
                    var payloads = _model1EventProcessor.Event1Details.NormalStage.ReceivedEvents.Select(e => e.Payload);
                    payloads.ShouldBe(args);
                }
            }

            public class BaseEventObservation : RouterTests
            {
                [Test]
                public void CanObserveEventsByBaseType()
                {
                    var receivedEventCount = 0;
                    _router
                        .GetEventObservable<BaseEvent, TestModel>(_model1.Id)
                        .Observe((e, c, m) => receivedEventCount++);
                    _router.PublishEvent(_model1.Id, new Event1());
                    _router.PublishEvent(_model1.Id, new Event2());
                    receivedEventCount.ShouldBe(2);
                }

                [Test]
                public void CanMergeEventStreams()
                {
                    var receivedEvents = new List<BaseEvent>();
                    var stream = EventObservable.Merge(
                        _router.GetEventObservable<BaseEvent, TestModel>(_model1.Id), // stream 1
                        _router.GetEventObservable<Event2, TestModel>(_model1.Id, ObservationStage.Preview), // stream 2
                        _router.GetEventObservable<Event3, TestModel>(_model1.Id) // stream 3
                    );
                    stream.Observe((baseEvent, context, model) =>
                    {
                        receivedEvents.Add(baseEvent);
                    });
                    _router.PublishEvent(_model1.Id, new Event1());
                    receivedEvents.Count.ShouldBe(1); // stream 1 should procure 
                    _router.PublishEvent(_model1.Id, new Event2());
                    receivedEvents.Count.ShouldBe(3); // stream 1 and 2 should procure 
                    _router.PublishEvent(_model1.Id, new Event3());
                    receivedEvents.Count.ShouldBe(5); // stream 1 and 3 should procure 
                }

                [Test]
                public void Issue45TestCase()
                {
                    var router = new Router<TestModel>(new TestModel());
                    router.GetEventObservable<BaseEvent>().Observe((ev, model) => Assert.Pass());
                    router.PublishEvent(new Event1());
                    Assert.Fail();
                }

                public class Foo<T> : BaseEvent
                { }

                [Test]
                public void Issue45TestCase_OpenGenerics()
                {
                    var router = new Router<TestModel>(new TestModel());
                    var stream = EventObservable.Merge<BaseEvent, IEventContext, TestModel>(
                        router.GetEventObservable<Foo<string>>(),
                        router.GetEventObservable<Foo<int>>()
                    );
                    stream.Observe((ev, model) => Assert.Pass());
                    router.PublishEvent(new Foo<string>());
                    Assert.Fail();
                }
            }

            public class ExecuteEvent : RouterTests
            {
                [Test]
                public void ThrowsIfExecutedOutsideOfEventLoop()
                {
                    InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => _router.ExecuteEvent(_model1.Id, new Event1()));
                    ex.Message.ShouldContain("Can't execute event.");
                }

                [Test]
                public void ThrowsIfExecutedFromPreProcessor()
                {
                    _model1PreEventProcessor.RegisterAction(m => _router.ExecuteEvent(_model1.Id, new Event3()));
                    AssertEventPublishThrows();
                }

                [Test]
                public void ThrowsIfExecutedFromAPostProcessor()
                {
                    _model1PostEventProcessor.RegisterAction(m => _router.ExecuteEvent(_model1.Id, new Event3()));
                    AssertEventPublishThrows();
                }

                [Test]
                public void ThrowsIfExecutedDuringModelUpdate()
                {
                    _model1Controller.RegisterAction(c => _router.ExecuteEvent(_model1.Id, new Event3()));
                    AssertEventPublishThrows();
                }

                [Test]
                public void ThrowsIfExecutedAgainstAnotherModel()
                {
                    _model1EventProcessor.Event1Details.NormalStage.RegisterAction((m, e) =>
                    {
                        _router.ExecuteEvent(_model2.Id, new Event3());
                    });
                    AssertEventPublishThrows();
                }

                [Test]
                public void ThrowsIfExecutedHandlerRaisesAnotherEvent()
                {
                    _model1EventProcessor.Event1Details.NormalStage.RegisterAction((m, e) =>
                    {
                        _router.ExecuteEvent(_model1.Id, new Event3());
                    });
                    _model1EventProcessor.Event3Details.NormalStage.RegisterAction((m, e) =>
                    {
                        _router.ExecuteEvent(_model1.Id, new Event1());
                    });
                    AssertEventPublishThrows();
                }

                [Test]
                public void ImmediatelyPublishesTheExecutedEventObservers()
                {
                    var passed = false;
                    _model1EventProcessor.Event1Details.NormalStage.RegisterAction((m, e) =>
                    {
                        _router.ExecuteEvent(_model1.Id, new Event3 { ShouldCommit = true, CommitAtStage = ObservationStage.Normal, CommitAtEventProcesserId = EventProcessor1Id });
                        passed = _model1EventProcessor.Event3Details.PreviewStage.ReceivedEvents.Count == 1;
                        passed = passed && _model1EventProcessor.Event3Details.NormalStage.ReceivedEvents.Count == 1;
                        passed = passed && _model1EventProcessor.Event3Details.CommittedStage.ReceivedEvents.Count == 1;
                    });
                    _router.PublishEvent(_model1.Id, new Event1());
                    passed.ShouldBe(true);
                    _model1PreEventProcessor.InvocationCount.ShouldBe(1);
                    _model1PostEventProcessor.InvocationCount.ShouldBe(1);
                }

                [Test]
                public void CanExecuteUsingObjectObjectOverload()
                {
                    var passed = false;
                    _model1EventProcessor.Event1Details.NormalStage.RegisterAction((m, e) =>
                    {
                        object event3 = new Event3();
                        _router.ExecuteEvent(_model1.Id, event3);
                        passed = _model1EventProcessor.Event3Details.NormalStage.ReceivedEvents.Count == 1;
                    });
                    _router.PublishEvent(_model1.Id, new Event1());
                    passed.ShouldBe(true);
                }

                private void AssertEventPublishThrows()
                {
                    _router.PublishEvent(_model1.Id, new Event1());
                    _terminalErrorHandler.Errors.Count.ShouldBe(1);
                    _terminalErrorHandler.Errors[0].ShouldBeOfType<InvalidOperationException>();
                    _terminalErrorHandler.Errors[0].Message.ShouldContain("Can't execute event");
                }
            }

            public class Broadcast : RouterTests
            {
                [Test]
                public void DeliversEventToAllModels()
                {
                    _router.BroadcastEvent(new Event1());
                    _model1EventProcessor.Event1Details.NormalStage.ReceivedEvents.Count.ShouldBe(1);
                    _model1Controller.ReceivedModels.Count.ShouldBe(1);

                    _model2EventProcessor.Event1Details.NormalStage.ReceivedEvents.Count.ShouldBe(1);
                    _model2Controller.ReceivedModels.Count.ShouldBe(1);
                }

                [Test]
                public void CanBroadcastUsingObjectOverload()
                {
                    _router.BroadcastEvent((object)new Event1());
                    _model1EventProcessor.Event1Details.NormalStage.ReceivedEvents.Count.ShouldBe(1);
                    _model1Controller.ReceivedModels.Count.ShouldBe(1);

                    _model2EventProcessor.Event1Details.NormalStage.ReceivedEvents.Count.ShouldBe(1);
                    _model2Controller.ReceivedModels.Count.ShouldBe(1);
                }
            }

            public class EventPublication : RouterTests
            {
                [Test]
                public void CanPublishUsingObjectOverload()
                {
                    _router.PublishEvent(_model1.Id, (object)new Event1());
                    _model1EventProcessor.Event1Details.NormalStage.ReceivedEvents.Count.ShouldBe(1);
                }
            }

            public class ModelChangedEvent : RouterTests
            {
                [Test]
                public void WhenEventProcessingWorkflowFinishedModelChangedEventIsRaised()
                {
                    var receivedEvents = new List<ModelChangedEvent<TestModel>>();
                    _router.GetEventObservable<ModelChangedEvent<TestModel>, TestModel3>(_model3.Id).Observe((e, c, m) =>
                    {
                        receivedEvents.Add(e);
                    });
                    _router.PublishEvent(_model1.Id, new Event1());
                    receivedEvents.Count.ShouldBe(1);
                    receivedEvents[0].Model.Id.ShouldBe(_model1.Id);
                    receivedEvents[0].ModelId.ShouldBe(_model1.Id);
                }

                [Test]
                public void ObservingTheSameModelTypesChangedEventThrows()
                {
                    _router.GetEventObservable<ModelChangedEvent<TestModel>, TestModel>(_model2.Id).Observe((e, c, m) =>
                    {
                    });
                    _router.PublishEvent(_model1.Id, new Event1());
                    _terminalErrorHandler.Errors.Count.ShouldBe(1);
                    _terminalErrorHandler.Errors[0].ShouldBeOfType<NotSupportedException>();
                }
            }

            public class ModelIdTypeMismatchErrors : RouterTests
            {
                [Test]
                public void GetEventObservableWithIncorrectModelTypeThrows()
                {
                    Assert.Throws<InvalidOperationException>(() => _router.GetEventObservable<string, string>(_model1.Id));
                }

                [Test]
                public void GetModelObservableWithIncorrectModelTypeThrows()
                {
                    Assert.Throws<InvalidOperationException>(() => _router.GetModelObservable<string>(_model1.Id));
                }
            }
        }
    }
}