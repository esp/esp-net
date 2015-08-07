using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Esp.Net.ModelRouter;
using Esp.Net.Reactive;
using NUnit.Framework;
using Shouldly;

namespace Esp.Net
{
    [TestFixture]
    public class RouterTests
    {
        private Router _router;
        private StubThreadGuard _threadGuard;

        private TestModel _model1;
        private StubModelProcessor _model1PreEventProcessor;
        private StubModelProcessor _model1PostEventProcessor;
        private GenericModelEventProcessor<TestModel> _model1EventProcessor;
        private GenericModelEventProcessor<TestModel> _model1EventProcessor2;
        private TestModelController _model1Controller;

        private TestModel _model2;
        private StubModelProcessor _model2PreEventProcessor;
        private StubModelProcessor _model2PostEventProcessor;
        private GenericModelEventProcessor<TestModel> _model2EventProcessor;
        private TestModelController _model2Controller;

        private TestModel3 _model3;
        private TestModel4 _model4;

        private const int EventProcessor1Id = 1;
        private const int EventProcessor2Id = 2;
        private const int EventProcessor3Id = 3;

        [SetUp]
        public virtual void SetUp()
        {
            _threadGuard = new StubThreadGuard();
            _router = new Router(_threadGuard);

            _model1 = new TestModel();
            _model1PreEventProcessor = new StubModelProcessor();
            _model1PostEventProcessor = new StubModelProcessor();
            _router.RegisterModel(_model1.Id, _model1, _model1PreEventProcessor, _model1PostEventProcessor);
            _model1EventProcessor = new GenericModelEventProcessor<TestModel>(_router, _model1.Id, EventProcessor1Id);
            _model1EventProcessor2 = new GenericModelEventProcessor<TestModel>(_router, _model1.Id, EventProcessor2Id);
            _model1Controller = new TestModelController(_router, _model1.Id);

            _model2 = new TestModel();
            _model2PreEventProcessor = new StubModelProcessor();
            _model2PostEventProcessor = new StubModelProcessor();
            _router.RegisterModel(_model2.Id, _model2, _model2PreEventProcessor, _model2PostEventProcessor);
            _model2EventProcessor = new GenericModelEventProcessor<TestModel>(_router, _model2.Id, EventProcessor3Id);
            _model2Controller = new TestModelController(_router, _model2.Id);

            _model3 = new TestModel3();
            _router.RegisterModel(_model3.Id, _model3);

            _model4 = new TestModel4();
            _router.RegisterModel(_model4.Id, _model4);
        }

        public class Ctor
        {
            [Test]
            public void ThrowsIfThreadGuardNull()
            {
                Assert.Throws<ArgumentNullException>(() => new Router(null));
            }
        }

        public class RegisterModel : RouterTests
        {
            [Test]
            public void ThrowsIfModelIdGuidEmpty()
            {
                Assert.Throws<ArgumentException>(() => _router.RegisterModel(Guid.Empty, new object()));
            }

            [Test]
            public void ThrowsIfPreEventProcessorNull()
            {
                Assert.Throws<ArgumentNullException>(() => _router.RegisterModel(Guid.NewGuid(), new object(), (IPreEventProcessor<object>)null));
            }

            [Test]
            public void ThrowsIfPostEventProcessorNull()
            {
                Assert.Throws<ArgumentNullException>(() => _router.RegisterModel(Guid.NewGuid(), new object(), (IPostEventProcessor<object>)null));
            }

            [Test]
            public void ThrowsIfModelNull()
            {
                Assert.Throws<ArgumentNullException>(() => _router.RegisterModel(Guid.NewGuid(), (object)null));
            }

            [Test]
            public void ThrowsIfModelAlreadyRegistered()
            {
                Assert.Throws<ArgumentException>(() => _router.RegisterModel(_model1.Id, new TestModel()));
            }
        }

        public class RemoveModel : RouterTests
        {
            [Test]
            public void EventObserversCompleteOnRemoval()
            {
                _router.RemoveModel(_model1.Id);
                
                _model1EventProcessor.Event1Details.PreviewStage.StreamCompletedCount.ShouldBe(1);
                _model1EventProcessor.Event1Details.NormalStage.StreamCompletedCount.ShouldBe(1);
                _model1EventProcessor.Event1Details.CommittedStage.StreamCompletedCount.ShouldBe(1);
                _model1EventProcessor.Event2Details.PreviewStage.StreamCompletedCount.ShouldBe(1);
                _model1EventProcessor.Event2Details.NormalStage.StreamCompletedCount.ShouldBe(1);
                _model1EventProcessor.Event2Details.CommittedStage.StreamCompletedCount.ShouldBe(1);
                      
                _model2EventProcessor.Event1Details.PreviewStage.StreamCompletedCount.ShouldBe(0);
                _model2EventProcessor.Event1Details.NormalStage.StreamCompletedCount.ShouldBe(0);
                _model2EventProcessor.Event1Details.CommittedStage.StreamCompletedCount.ShouldBe(0);
                _model2EventProcessor.Event2Details.PreviewStage.StreamCompletedCount.ShouldBe(0);
                _model2EventProcessor.Event2Details.NormalStage.StreamCompletedCount.ShouldBe(0);
                _model2EventProcessor.Event2Details.CommittedStage.StreamCompletedCount.ShouldBe(0);

                _router.RemoveModel(_model2.Id);

                _model2EventProcessor.Event1Details.PreviewStage.StreamCompletedCount.ShouldBe(1);
                _model2EventProcessor.Event1Details.NormalStage.StreamCompletedCount.ShouldBe(1);
                _model2EventProcessor.Event1Details.CommittedStage.StreamCompletedCount.ShouldBe(1);
                _model2EventProcessor.Event2Details.PreviewStage.StreamCompletedCount.ShouldBe(1);
                _model2EventProcessor.Event2Details.NormalStage.StreamCompletedCount.ShouldBe(1);
                _model2EventProcessor.Event2Details.CommittedStage.StreamCompletedCount.ShouldBe(1);
            }

            [Test]
            public void ModelObserversCompleteOnRemoval()
            {
                _router.RemoveModel(_model1.Id);
                _model1Controller.StreamCompletedCount.ShouldBe(1);
                _model2Controller.StreamCompletedCount.ShouldBe(0);

                _router.RemoveModel(_model2.Id);
                _model2Controller.StreamCompletedCount.ShouldBe(1);
            }

            [Test]
            public void QueuedEventsAreIgnoredOnRemoval()
            {
                _model1EventProcessor.Event2Details.NormalStage.RegisterAction((m, e) =>
                {
                    _router.PublishEvent(_model1.Id, new Event1());
                    _router.RemoveModel(_model1.Id);
                });
                _router.PublishEvent(_model1.Id, new Event2());
                _model1EventProcessor.Event1Details.NormalStage.ReceivedEvents.Count.ShouldBe(0);
            }

            [Test]
            public void RemovalByAPreProcessorEndsEventWorkflow()
            {
                _model1PreEventProcessor.RegisterAction(model =>
                {
                    _router.RemoveModel(_model1.Id);
                });
                _router.PublishEvent(_model1.Id, new Event1());
                _model1EventProcessor.Event1Details.PreviewStage.ReceivedEvents.Count.ShouldBe(0);
                _model1EventProcessor.Event1Details.NormalStage.ReceivedEvents.Count.ShouldBe(0);
                _model1EventProcessor.Event1Details.CommittedStage.ReceivedEvents.Count.ShouldBe(0);
                _model1Controller.ReceivedModels.Count.ShouldBe(0);
            }

            [Test]
            public void RemovalAtPreviewStageEndsEventWorkflow()
            {
                var event1 = new Event1
                {
                    ShouldRemove = true,
                    RemoveAtStage = ObservationStage.Preview,
                    RemoveAtEventProcesserId = EventProcessor1Id,
                };
                _router.PublishEvent(_model1.Id, event1);
                _model1EventProcessor.Event1Details.PreviewStage.ReceivedEvents.Count.ShouldBe(1);
                _model1EventProcessor.Event1Details.NormalStage.ReceivedEvents.Count.ShouldBe(0);
                _model1Controller.ReceivedModels.Count.ShouldBe(0);
            }

            [Test]
            public void RemovalAtNormalStageEndsEventWorkflow()
            {
                var event1 = new Event1
                {
                    ShouldRemove = true, 
                    RemoveAtStage = ObservationStage.Normal, 
                    RemoveAtEventProcesserId = EventProcessor1Id,
                    
                    ShouldCommit = true, 
                    CommitAtStage = ObservationStage.Normal,
                    CommitAtEventProcesserId = EventProcessor1Id
                };
                _router.PublishEvent(_model1.Id, event1);
                _model1EventProcessor.Event1Details.PreviewStage.ReceivedEvents.Count.ShouldBe(1);
                _model1EventProcessor.Event1Details.NormalStage.ReceivedEvents.Count.ShouldBe(1);
                _model1EventProcessor.Event1Details.CommittedStage.ReceivedEvents.Count.ShouldBe(0);
                _model1Controller.ReceivedModels.Count.ShouldBe(0);
            }

            [Test]
            public void RemovalAtCommittedStageEndsEventWorkflow()
            {
                var event1 = new Event1()
                {
                    ShouldRemove = true, 
                    RemoveAtStage = ObservationStage.Committed, 
                    RemoveAtEventProcesserId = EventProcessor1Id,
                    
                    ShouldCommit = true, 
                    CommitAtStage = ObservationStage.Normal, 
                    CommitAtEventProcesserId = EventProcessor1Id
                };
                _router.PublishEvent(_model1.Id, event1);
                _model1EventProcessor.Event1Details.CommittedStage.ReceivedEvents.Count.ShouldBe(1);
                _model1Controller.ReceivedModels.Count.ShouldBe(0);
            }

            [Test]
            public void RemovalByAPostProcessorEndsEventWorkflow()
            {
                _model1PostEventProcessor.RegisterAction(model =>
                {
                    _router.RemoveModel(_model1.Id);
                });
                _router.PublishEvent(_model1.Id, new Event1());
                _model1EventProcessor.Event1Details.NormalStage.ReceivedEvents.Count.ShouldBe(1);
                _model1Controller.ReceivedModels.Count.ShouldBe(0);
            }

            [Test]
            public void RemovalByAModelObserverEndsEventWorkflow()
            {
                var receivedModelCount = 0;
                _model1EventProcessor.Event1Details.NormalStage.RegisterAction((m, e) => m.ControllerShouldRemove = true);
                _router.GetModelObservable<TestModel>(_model1.Id).Observe(model => receivedModelCount++);
                _router.PublishEvent(_model1.Id, new Event1());
                _model1Controller.ReceivedModels.Count.ShouldBe(1);
                receivedModelCount.ShouldBe(0);
            }
        }

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
                _router.PublishEvent(_model1.Id, new Event1 { ShouldCommit = true, CommitAtStage = ObservationStage.Normal, CommitAtEventProcesserId = EventProcessor2Id});
                order.ShouldBe(new [] {1, 2, 3, 4, 5});
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
                        if(e.Payload != "B") _router.PublishEvent(_model1.Id, new Event1("B"));
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
                        .GetEventObservable<TestModel, BaseEvent>(_model1.Id, typeof (Event1))
                        .Observe((m, e) => receivedEventCount++);
                    _router.PublishEvent(_model1.Id, new Event1());
                    receivedEventCount.ShouldBe(1);
                }

                [Test]
                public void CanObserveEventsByBaseTypeUsingGenericTypeOverload()
                {
                    var receivedEventCount = 0;
                    _router
                        .GetEventObservable<TestModel, Event1, BaseEvent>(_model1.Id)
                        .Observe((m, e) => receivedEventCount++);
                    _router.PublishEvent(_model1.Id, new Event1());
                    receivedEventCount.ShouldBe(1);
                }

                [Test]
                public void ThrowsIfSubTypeDoesntDeriveFromBase()
                {
                    Assert.Throws<ArgumentException>(() => _router.GetEventObservable<TestModel, BaseEvent>(_model1.Id, typeof (string)));
                }

                [Test]
                public void CanConcatEventStreams()
                {
                    var receivedEvents = new List<BaseEvent>();
                    var stream = EventObservable.Concat(
                        _router.GetEventObservable<TestModel, BaseEvent>(_model1.Id, typeof(Event1)),
                        _router.GetEventObservable<TestModel, BaseEvent>(_model1.Id, typeof(Event2)),
                        _router.GetEventObservable<TestModel, BaseEvent>(_model1.Id, typeof(Event3))
                    );
                    stream.Observe((model, baseEvent, context) =>
                    {
                        receivedEvents.Add(baseEvent);
                    });
                    _router.PublishEvent(_model1.Id, new Event1());
                    _router.PublishEvent(_model1.Id, new Event2());
                    _router.PublishEvent(_model1.Id, new Event3());
                    receivedEvents.Count.ShouldBe(3);
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
                        _router.ExecuteEvent(_model1.Id, new Event3 { ShouldCommit = true, CommitAtStage = ObservationStage.Normal, CommitAtEventProcesserId = EventProcessor1Id});
                        passed = _model1EventProcessor.Event3Details.PreviewStage.ReceivedEvents.Count == 1;
                        passed = passed && _model1EventProcessor.Event3Details.NormalStage.ReceivedEvents.Count == 1;
                        passed = passed && _model1EventProcessor.Event3Details.CommittedStage.ReceivedEvents.Count == 1;
                    });
                    _router.PublishEvent(_model1.Id, new Event1());
                    passed.ShouldBe(true);
                    _model1PreEventProcessor.InvocationCount.ShouldBe(1);
                    _model1PostEventProcessor.InvocationCount.ShouldBe(1);
                }

                private void AssertEventPublishThrows()
                {
                    InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => _router.PublishEvent(_model1.Id, new Event1()));
                    ex.Message.ShouldContain("Can't execute event.");
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
                    _router.GetEventObservable<TestModel3, ModelChangedEvent<TestModel>>(_model3.Id).Observe((m, e) =>
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
                    _router.GetEventObservable<TestModel, ModelChangedEvent<TestModel>>(_model2.Id).Observe((m, e) =>
                    {
                    });
                    Assert.Throws<NotSupportedException>(() => _router.PublishEvent(_model1.Id, new Event1()));
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

        public class EventObservationDisposal : RouterTests
        {
            [Test]
            public void DisposedPreviewObservationStageObserversDontRecievEvent()
            {
                _model1EventProcessor.Event1Details.PreviewStage.ObservationDisposable.Dispose();
                _router.PublishEvent(_model1.Id, new Event1());
                _model1EventProcessor.Event1Details.PreviewStage.ReceivedEvents.Count.ShouldBe(0);
                _model1EventProcessor.Event1Details.NormalStage.ReceivedEvents.Count.ShouldBe(1);
            }

            [Test]
            public void DisposedNormalObservationStageObserversDontRecievEvent()
            {
                _model1EventProcessor.Event1Details.NormalStage.ObservationDisposable.Dispose();
                _router.PublishEvent(_model1.Id, new Event1());
                _model1EventProcessor.Event1Details.PreviewStage.ReceivedEvents.Count.ShouldBe(1);
                _model1EventProcessor.Event1Details.NormalStage.ReceivedEvents.Count.ShouldBe(0);
            }

            [Test]
            public void DisposedCommittedObservationStageObserversDontRecievEvent()
            {
                _model1EventProcessor.Event1Details.CommittedStage.ObservationDisposable.Dispose();
                _router.PublishEvent(_model1.Id, new Event1{ ShouldCommit = true, CommitAtStage = ObservationStage.Normal, CommitAtEventProcesserId = EventProcessor1Id });
                _model1EventProcessor.Event1Details.NormalStage.ReceivedEvents.Count.ShouldBe(1);
                _model1EventProcessor.Event1Details.CommittedStage.ReceivedEvents.Count.ShouldBe(0);
            }
        }

        public class ErrorFlows : RouterTests
        {
            [Test]
            public void CancelingAtNormalObservationStageThrows()
            {
                Assert.Throws<InvalidOperationException>(() =>
                {
                    var event1 = new Event1
                    {
                        ShouldCancel = true,
                        CancelAtStage = ObservationStage.Normal,
                        CancelAtEventProcesserId = EventProcessor1Id,
                    };
                    _router.PublishEvent(_model1.Id, event1);
                });
            }

            [Test]
            public void CancelingAtCommittedObservationStageThrows()
            {
                Assert.Throws<InvalidOperationException>(() =>
                {
                    var event1 = new Event1
                    {
                        ShouldCommit = true, 
                        CommitAtStage = ObservationStage.Normal, 
                        CommitAtEventProcesserId = EventProcessor1Id,

                        ShouldCancel = true,
                        CancelAtStage = ObservationStage.Committed,
                        CancelAtEventProcesserId = EventProcessor1Id,
                    };
                    _router.PublishEvent(_model1.Id, event1);
                });
            }

            [Test]
            public void CommittingAtPreviewObservationStageThrows()
            {
                Assert.Throws<InvalidOperationException>(() =>
                {
                    var event1 = new Event1
                    {
                        ShouldCommit = true,
                        CommitAtStage = ObservationStage.Preview,
                        CommitAtEventProcesserId = EventProcessor1Id,
                    };
                    _router.PublishEvent(_model1.Id, event1);
                });
            }
        }

        public class ModelObservation : RouterTests
        {
            [Test]
            public void ThrowsIfModelIdGuidEmpty()
            {
                Assert.Throws<ArgumentException>(() => _router.GetModelObservable<TestModel>(Guid.Empty));
            }

            [Test]
            public void ObserversReceiveModelOnEventWorkflowCompleted()
            {
                _router.PublishEvent(_model1.Id, new Event1());
                _model1Controller.ReceivedModels.Count.ShouldBe(1);
            }

            [Test]
            public void DisposedObserversReceiveDontModelOnEventWorkflowCompleted()
            {
                _model1Controller.ModelObservationDisposable.Dispose();
                _router.PublishEvent(_model1.Id, new Event1());
                _model1Controller.ReceivedModels.Count.ShouldBe(0);
            }

            [Test]
            public void MutipleSubsequentEventsOnlyYield1ModelUpdate()
            {
                PublishEventWithMultipeSubsequentEvents(5);
                _model1EventProcessor.Event1Details.NormalStage.ReceivedEvents.Count.ShouldBe(6);
                _model1Controller.ReceivedModels.Count.ShouldBe(1);
            }

            [Test]
            public void EventsPublishedDuringModelDispatchGetProcessed()
            {
                bool publishedEventFromController = false;
                _model1Controller.RegisterAction(m =>
                {
                    if (!publishedEventFromController)
                    {
                        publishedEventFromController = true;
                        _router.PublishEvent(_model1.Id, new Event1());
                    }
                });
                _router.PublishEvent(_model1.Id, new Event1());
                publishedEventFromController.ShouldBe(true);
                _model1Controller.ReceivedModels.Count.ShouldBe(2);
            }

            public class ModelCloning : RouterTests
            {
                [Test]
                public void DispatchesAModelCloneIfTheModelImplementsIClonable()
                {
                    var receivedModels = new List<TestModel4>();
                    _router.GetEventObservable<TestModel4, int>(_model4.Id).Observe((m, e) => { /*noop*/ });
                    _router.GetModelObservable<TestModel4>(_model4.Id).Observe(m => receivedModels.Add(m));
                    _router.PublishEvent(_model4.Id, 2);
                    _router.PublishEvent(_model4.Id, 4);
                    receivedModels.Count.ShouldBe(2);
                    receivedModels[0].IsClone.ShouldBe(true);
                    receivedModels[1].IsClone.ShouldBe(true);
                }
            }
        }

        public class ModelRouter : RouterTests
        {
            private IRouter<TestModel> _modelRouter;

            [SetUp]
            public override void SetUp()
            {
                base.SetUp();
                _modelRouter = _router.CreateModelRouter<TestModel>(_model1.Id);
            }

            [Test]
            public void CanPublishAndObserveProxiedEvent()
            {
                var receivedEventCount = 0;
                _modelRouter.GetEventObservable<Event1>().Observe((m, e, c) => receivedEventCount++);
                _modelRouter.PublishEvent(new Event1());
                receivedEventCount.ShouldBe(1);
            }

            [Test]
            public void CanObserveProxiedModel()
            {
                var receivedModelCount = 0;
                _modelRouter.GetModelObservable().Observe(m => receivedModelCount++);
                _modelRouter.PublishEvent(new Event1());
                receivedModelCount.ShouldBe(1);
            }

            [Test]
            public void CanBroadcastProxiedEvent()
            {
                _modelRouter.BroadcastEvent(new Event1());
                _model1EventProcessor.Event1Details.NormalStage.ReceivedEvents.Count.ShouldBe(1);
                _model2EventProcessor.Event1Details.NormalStage.ReceivedEvents.Count.ShouldBe(1);
            }

            [Test]
            public void CanExeuteProxiedEvent()
            {
                Assert.Inconclusive();
            }
        }

        public class ThreadGuard : RouterTests
        {
            [SetUp]
            public override void SetUp()
            {
                base.SetUp();
                _threadGuard.HasAccess = false;
            }

            [Test]
            public void ShouldThrowIfRegisterModelCalledOnInvalidThread()
            {
                Assert.Throws<InvalidOperationException>(() => _router.RegisterModel(Guid.NewGuid(), _model1));
                Assert.Throws<InvalidOperationException>(() => _router.RegisterModel(Guid.NewGuid(), _model1, (IPreEventProcessor<TestModel>)new StubModelProcessor()));
                Assert.Throws<InvalidOperationException>(() => _router.RegisterModel(Guid.NewGuid(), _model1, (IPostEventProcessor<TestModel>)new StubModelProcessor()));
                Assert.Throws<InvalidOperationException>(() => _router.RegisterModel(Guid.NewGuid(), _model1, new StubModelProcessor(), new StubModelProcessor()));
            }

            [Test]
            public void ShouldThrowIfRemoveModelCalledOnInvalidThread()
            {
                Assert.Throws<InvalidOperationException>(() => _router.RemoveModel(_model1.Id));
            }

            [Test]
            public void ShouldThrowIfPublishEventCalledOnInvalidThread()
            {
                Assert.Throws<InvalidOperationException>(() => _router.PublishEvent(_model1.Id, new Event1()));
                Assert.Throws<InvalidOperationException>(() => _router.PublishEvent(_model1.Id, (object)new Event1()));
            }

            [Test]
            public void ShouldThrowIfBroadcastEventCalledOnInvalidThread()
            {
                Assert.Throws<InvalidOperationException>(() => _router.BroadcastEvent(new Event1()));
                Assert.Throws<InvalidOperationException>(() => _router.BroadcastEvent((object)new Event1()));
            }

            [Test]
            public void ShouldThrowIfGetModelObservableCalledOnInvalidThread()
            {
                Assert.Throws<InvalidOperationException>(() => _router.GetModelObservable<TestModel>(_model1.Id));
                _threadGuard.HasAccess = true;
                var obs = _router.GetModelObservable<TestModel>(_model1.Id);
                _threadGuard.HasAccess = false;
                Assert.Throws<InvalidOperationException>(() => obs.Observe(m => { }));
            }

            [Test]
            public void ShouldThrowIfGetEventObservableCalledOnInvalidThread()
            {
                Assert.Throws<InvalidOperationException>(() => _router.GetEventObservable<TestModel, Event1>(_model1.Id));
                _threadGuard.HasAccess = true;
                var obs = _router.GetEventObservable<TestModel, Event1>(_model1.Id);
                _threadGuard.HasAccess = false;
                Assert.Throws<InvalidOperationException>(() => obs.Observe((m, e) => { }));
            }
        }

        public class RouterHalting : RouterTests
        {
            [Test]
            public void ShouldHaltAndRethrowIfAPreProcessorErrors()
            {
                _model1EventProcessor.Event1Details.NormalStage.RegisterAction((m, e) =>
                {
                    throw new Exception("Boom");
                });
                AssertPublishEventThrows();
            }

            [Test]
            public void ShouldHaltAndRethrowIfAnEventProcessorErrors()
            {
                _model1PreEventProcessor.RegisterAction(m =>
                {
                    throw new Exception("Boom");
                });
                AssertPublishEventThrows();
            }

            [Test]
            public void ShouldHaltAndRethrowIfAnPostProcessorErrors()
            {
                _model1PostEventProcessor.RegisterAction(m =>
                {
                    throw new Exception("Boom");
                });
                AssertPublishEventThrows();
            }

            [Test]
            public void ShouldHaltAndRethrowIfAModelObserverErrors()
            {
                _model1Controller.RegisterAction(m =>
                {
                    throw new Exception("Boom");
                });
                AssertPublishEventThrows();
            }

            private void AssertPublishEventThrows()
            {
                Exception ex = Assert.Throws<Exception>(() => _router.PublishEvent(_model1.Id, new Event1()));
                ex.Message.ShouldBe("Boom");
                Exception ex2 = Assert.Throws<Exception>(() => _router.PublishEvent(_model1.Id, new Event2()));
                ex2.Message.ShouldBe("Router halted due to previous error");
                ex2.InnerException.Message.ShouldBe("Boom");
            }

            public class WhenHalted : RouterTests
            {
                [SetUp]
                public override void SetUp()
                {
                    base.SetUp();
                    var exceptionThrow = false;
                    _model1PreEventProcessor.RegisterAction(m =>
                    {
                        if (!exceptionThrow)
                        {
                            exceptionThrow = true;
                            throw new Exception("Boom");
                        }
                    });
                    try
                    {
                        _router.PublishEvent(_model1.Id, new Event1());
                    }
                    catch
                    {
                    }
                }

                [Test]
                public void ShouldThrowOnRegisterModel()
                {
                    AssertRethrows(() => _router.RegisterModel(Guid.NewGuid(), _model1));
                    AssertRethrows(() => _router.RegisterModel(Guid.NewGuid(), _model1, (IPreEventProcessor<TestModel>)new StubModelProcessor()));
                    AssertRethrows(() => _router.RegisterModel(Guid.NewGuid(), _model1, (IPostEventProcessor<TestModel>)new StubModelProcessor()));
                    AssertRethrows(() => _router.RegisterModel(Guid.NewGuid(), _model1, new StubModelProcessor(), new StubModelProcessor()));
                }

                [Test]
                public void ShouldThrowOnCreateModelRouter()
                {
                    AssertRethrows(() => _router.CreateModelRouter<TestModel>(_model1.Id));
                }

                [Test]
                public void ShouldThrowOnGetModelObservable()
                {
                    AssertRethrows(() => _router.GetModelObservable<TestModel>(_model1.Id));
                }

                [Test]
                public void ShouldThrowOnGetEventObservable()
                {
                    AssertRethrows(() => _router.GetEventObservable<TestModel, Event1>(_model1.Id));
                }

                [Test]
                public void ShouldThrowOnPublishEvent()
                {
                    AssertRethrows(() => _router.PublishEvent(_model1.Id, new Event2()));
                    AssertRethrows(() => _router.PublishEvent(_model1.Id, (object)new Event2()));
                }

                [Test]
                public void ShouldThrowOnExecuteEvent()
                {
                    Assert.Inconclusive();
                    // AssertRethrows(() => _router.ExecuteEvent(_model1.Id, new Event2()));
                }

                [Test]
                public void ShouldThrowOnBroadcastEvent()
                {
                    AssertRethrows(() => _router.BroadcastEvent(new Event2()));
                    AssertRethrows(() => _router.BroadcastEvent((object)new Event2()));
                }

                private void AssertRethrows(TestDelegate action)
                {
                    Exception ex = Assert.Throws<Exception>(action);
                    ex.Message.ShouldBe("Router halted due to previous error");
                    ex.InnerException.Message.ShouldBe("Boom");
                }
            }
        }

        protected void PublishEventWithMultipeSubsequentEvents(int numberOfSubsequentEvents)
        {
            _router
                .GetEventObservable<TestModel, Event1>(_model1.Id)
                .Where((m, e) => e.Payload != "subsequent")
                .Observe(
                    (model, @event) =>
                    {
                        for (int i = 0; i < numberOfSubsequentEvents; i++)
                        {
                            _router.PublishEvent(_model1.Id, new Event1("subsequent"));
                        }
                    }
                );
            _router.PublishEvent(_model1.Id, new Event1());
        }

        public class TestModel
        {
            public TestModel()
            {
                Id = Guid.NewGuid();
            }
            public Guid Id { get; private set; }
            public bool ControllerShouldRemove { get; set; }
        }

        public class TestModel3
        {
            public TestModel3()
            {
                Id = Guid.NewGuid();
            }
            public Guid Id { get; private set; }
        }

        public class TestModel4 : ICloneable<TestModel4>
        {
            public TestModel4()
            {
                Id = Guid.NewGuid();
            }
            
            public Guid Id { get; private set; }

            public bool IsClone { get; private set; }

            public TestModel4 Clone()
            {
                return new TestModel4() { Id = Id, IsClone = true };
            }
        }

        public class BaseEvent
        {
            public bool ShouldCancel { get; set; }
            public ObservationStage CancelAtStage { get; set; }
            public int CancelAtEventProcesserId { get; set; }

            public bool ShouldCommit { get; set; }
            public ObservationStage CommitAtStage { get; set; }
            public int CommitAtEventProcesserId { get; set; }

            public bool ShouldRemove { get; set; }
            public ObservationStage RemoveAtStage { get; set; }
            public int RemoveAtEventProcesserId { get; set; }
        }

        public class Event1 : BaseEvent
        {
            public Event1()
            {
            }

            public Event1(string payload)
            {
                Payload = payload;
            }

            public string Payload { get; private set; }
        }

        public class Event2 : BaseEvent { }

        public class Event3 : BaseEvent { }

        public class AnExecutedEvent
        {
            int Payload { get; set; }
        }

        public class StubModelProcessor : IPreEventProcessor<TestModel>, IPostEventProcessor<TestModel>
        {
            private readonly List<Action<TestModel>> _actions = new List<Action<TestModel>>();

            public int InvocationCount { get; private set; }

            public void Process(TestModel model)
            {
                InvocationCount++;

                foreach (Action<TestModel> action in _actions)
                {
                    action(model);
                }
            }

            public void RegisterAction(Action<TestModel> action)
            {
                _actions.Add(action);
            }
        }

        // this generic event processor exists to:  
        // * record what events it receives during execution
        // * push events through the flow as requested by tests
        // * run actions during the workflow as requested by the tests
        public class GenericModelEventProcessor<TModel>
        {
            private readonly Guid _modelId;
            private readonly int _id;
            private readonly IRouter _router;

            public GenericModelEventProcessor(IRouter router, Guid modelId, int id)
            {
                _modelId = modelId;
                _id = id;
                _router = router;
                
                Event1Details = ObserveEvent<Event1>();
                Event2Details = ObserveEvent<Event2>();
                Event3Details = ObserveEvent<Event3>();
            }

            public EventObservationDetails<Event1> Event1Details { get; private set; }
            public EventObservationDetails<Event2> Event2Details { get; private set; }
            public EventObservationDetails<Event3> Event3Details { get; private set; }

            private EventObservationDetails<TEvent> ObserveEvent<TEvent>() where TEvent : BaseEvent
            {
                var observationDetails = new EventObservationDetails<TEvent>
                {
                    PreviewStage = WireUpObservationStage<TEvent>(ObservationStage.Preview),
                    NormalStage = WireUpObservationStage<TEvent>(ObservationStage.Normal),
                    CommittedStage = WireUpObservationStage<TEvent>(ObservationStage.Committed)
                };
                return observationDetails;
            }

            private EventObservationStageDetails<TEvent> WireUpObservationStage<TEvent>(ObservationStage stage) where TEvent : BaseEvent
            {
                var details = new EventObservationStageDetails<TEvent>(stage);
                details.ObservationDisposable = _router.GetEventObservable<TModel, TEvent>(_modelId, details.Stage)
                    .Observe(
                        (model, @event, context) =>
                        {
                            details.ReceivedEvents.Add(@event);
                            var shouldCancel = @event.ShouldCancel && stage == @event.CancelAtStage && @event.CancelAtEventProcesserId == _id;
                            if (shouldCancel)
                            {
                                context.Cancel();
                            }
                            var shouldCommit = @event.ShouldCommit && stage == @event.CommitAtStage && @event.CommitAtEventProcesserId == _id;
                            if (shouldCommit)
                            {
                                context.Commit();
                            }
                            var shouldRemove = @event.ShouldRemove && stage == @event.RemoveAtStage && @event.RemoveAtEventProcesserId == _id;
                            if (shouldRemove)
                            {
                                _router.RemoveModel(_modelId);
                            }
                            foreach (Action<TModel, TEvent, IEventContext> action in details.Actions)
                            {
                                action(model, @event, context);
                            }
                        },
                        () => details.StreamCompletedCount++);
                return details;
            }

            public class EventObservationDetails<TEvent>
            {
                public EventObservationStageDetails<TEvent> PreviewStage { get; set; }
                public EventObservationStageDetails<TEvent> NormalStage { get; set; }
                public EventObservationStageDetails<TEvent> CommittedStage { get; set; }
            }

            public class EventObservationStageDetails<TEvent>
            {
                private readonly List<Action<TModel, TEvent, IEventContext>> _actions;
                public EventObservationStageDetails(ObservationStage stage)
                {
                    Stage = stage;
                    ReceivedEvents = new List<TEvent>();
                    _actions = new List<Action<TModel, TEvent, IEventContext>>();
                    Actions = new ReadOnlyCollection<Action<TModel, TEvent, IEventContext>>(_actions);
                }
                public ObservationStage Stage { get; private set; }
                public List<TEvent> ReceivedEvents { get; private set; }
                public IDisposable ObservationDisposable { get; set; }
                public int StreamCompletedCount { get; set; }
                public IList<Action<TModel, TEvent, IEventContext>> Actions { get; private set; }
                public void RegisterAction(Action<TModel, TEvent> action)
                {
                    _actions.Add((m, e, c) => action(m, e));
                }
                public void RegisterAction(Action<TModel, TEvent, IEventContext> action)
                {
                    _actions.Add(action);
                }
            }
        }

        public class TestModelController
        {
            private readonly List<Action<TestModel>> _actions = new List<Action<TestModel>>();

            public TestModelController(IRouter router, Guid modelId)
            {
                ReceivedModels = new List<TestModel>();
                ModelObservationDisposable = router
                    .GetModelObservable<TestModel>(modelId)
                    .Observe(
                        model =>
                        {
                            ReceivedModels.Add(model);
                            if (model.ControllerShouldRemove)
                               router.RemoveModel(modelId);
                            foreach (Action<TestModel> action in _actions)
                            {
                                action(model);
                            }
                        },
                        () =>
                        {
                            StreamCompletedCount++;
                        }
                    );
            }

            public IDisposable ModelObservationDisposable { get; private set; }

            public List<TestModel> ReceivedModels { get; private set; }
          
            public int StreamCompletedCount { get; private set; }

            public void RegisterAction(Action<TestModel> action)
            {
                _actions.Add(action);
            }
        }

        public class StubThreadGuard : IThreadGuard
        {
            public StubThreadGuard()
            {
                HasAccess = true;
            }

            public bool HasAccess { get; set; }

            public bool CheckAccess()
            {
                return HasAccess;
            }
        }
    }
}