using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Esp.Net.Reactive;
using NUnit.Framework;
using Shouldly;

namespace Esp.Net
{
    [TestFixture]
    public class RouterTests2
    {
        private Router _router;
        private StubThreadGuard _threadGuard;

        private TestModel _model1;
        private StubModelProcessor _model1PreEventProcessor;
        private StubModelProcessor _model1PostEventProcessor;
        private TestModelEventProcessor _model1EventProcessor;
        private TestModelEventProcessor _model1EventProcessor2;
        private TestController _model1Controller;

        private TestModel _model2;
        private StubModelProcessor _model2PreEventProcessor;
        private StubModelProcessor _model2PostEventProcessor;
        private TestModelEventProcessor _model2EventProcessor;
        private TestController _model2Controller;

        private const int EventProcessor1Id = 1;
        private const int EventProcessor2Id = 2;
        private const int EventProcessor3Id = 3;

        [SetUp]
        public void SetUp()
        {
            _threadGuard = new StubThreadGuard();
            _router = new Router(_threadGuard);

            _model1 = new TestModel();
            _model1PreEventProcessor = new StubModelProcessor();
            _model1PostEventProcessor = new StubModelProcessor();
            _router.RegisterModel(_model1.Id, _model1, _model1PreEventProcessor, _model1PostEventProcessor);
            _model1EventProcessor = new TestModelEventProcessor(_router, _model1.Id, EventProcessor1Id);
            _model1EventProcessor2 = new TestModelEventProcessor(_router, _model1.Id, EventProcessor2Id);
            _model1Controller = new TestController(_router, _model1.Id);

            _model2 = new TestModel();
            _model2PreEventProcessor = new StubModelProcessor();
            _model2PostEventProcessor = new StubModelProcessor();
            _router.RegisterModel(_model2.Id, _model2, _model2PreEventProcessor, _model2PostEventProcessor);
            _model2EventProcessor = new TestModelEventProcessor(_router, _model2.Id, EventProcessor3Id);
            _model2Controller = new TestController(_router, _model2.Id);
        }

        public class Ctor
        {
            [Test]
            public void ThrowsIfThreadGuardNull()
            {
                Assert.Throws<ArgumentNullException>(() => new Router(null));
            }
        }

        public class RegisterModel : RouterTests2
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

        public class RemoveModel : RouterTests2
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
                _router.PublishEvent(_model1.Id, new Event1() { ShouldRemove = true, RemoveAtStage = ObservationStage.Preview, RemoveAtEventProcesserId = EventProcessor1Id });
                _model1EventProcessor.Event1Details.PreviewStage.ReceivedEvents.Count.ShouldBe(1);
                _model1EventProcessor.Event1Details.NormalStage.ReceivedEvents.Count.ShouldBe(0);
                _model1EventProcessor.Event1Details.CommittedStage.ReceivedEvents.Count.ShouldBe(0);
                _model1Controller.ReceivedModels.Count.ShouldBe(0);
            }

            [Test]
            public void RemovalAtNormalStageEndsEventWorkflow()
            {
                _router.PublishEvent(_model1.Id, new Event1() { ShouldRemove = true, RemoveAtStage = ObservationStage.Normal, ShouldCommit = true, RemoveAtEventProcesserId = EventProcessor1Id });
                _model1EventProcessor.Event1Details.PreviewStage.ReceivedEvents.Count.ShouldBe(1);
                _model1EventProcessor.Event1Details.NormalStage.ReceivedEvents.Count.ShouldBe(1);
                _model1EventProcessor.Event1Details.CommittedStage.ReceivedEvents.Count.ShouldBe(0);
                _model1Controller.ReceivedModels.Count.ShouldBe(0);
            }

            [Test]
            public void RemovalAtCommittedStageEndsEventWorkflow()
            {
                _router.PublishEvent(_model1.Id, new Event1() { ShouldRemove = true, RemoveAtStage = ObservationStage.Committed, ShouldCommit = true, RemoveAtEventProcesserId = EventProcessor1Id });
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
                _router.GetModelObservable<TestModel>(_model1.Id).Observe(model => receivedModelCount++);
                _router.PublishEvent(_model1.Id, new Event1());
                _router.PublishEvent(_model1.Id, new Event1() { ShouldRemoveOnModelObservation = true });
                _model1Controller.ReceivedModels.Count.ShouldBe(2);
                receivedModelCount.ShouldBe(1);
            }
        }

        public class EventWorkflow : RouterTests2
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
                _router.PublishEvent(_model1.Id, new Event1 { ShouldCommit = true, CommitAtEventProcesserId = EventProcessor1Id });
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

            public class SubsequentEvents : RouterTests2
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

            public class ExecuteEvent
            {
                [Test]
                public void OnlyAllowsExecuteDuringEventWorkflowStages()
                {
                }

                [Test]
                public void ThrowsIfExecuteHandlerRaisesAnotherEvent()
                {
                }

                [Test]
                public void ImmediatelyPublishesTheExecutedEventToPreviewObservationStageObservers()
                {
                }

                [Test]
                public void ImmediatelyPublishesTheExecutedEventToNormalObservationStageObservers()
                {
                }

                [Test]
                public void ImmediatelyPublishesTheExecutedEventToCommittedObservationStageObservers()
                {
                }
            }

            public class Broadcast
            {
                [Test]
                public void DeliversEventToAllModels()
                { }
            }

            public class ModelChangedEvent : RouterTests2
            {
                [Test]
                public void ThrowsIfEventProcessorSubscribesToOwnModelChangedEvent()
                {
                }

                [Test]
                public void WhenEventProcessingWorkflowFinishedModelChangedEventIsRaised()
                {
                    _router.PublishEvent(_model1.Id, new Event1());
                    _model1EventProcessor.ModelChangedEvents.Count.ShouldBe(0);
                    _model2EventProcessor.ModelChangedEvents.Count.ShouldBe(1);
                }
            }

            // pre
            // staged
            // post
        }

        public class EventObservationDisposal
        {
            [Test]
            public void DisposedPreviewObservationStageObserversDontRecievEvent()
            {
            }

            [Test]
            public void DisposedNormalObservationStageObserversDontRecievEvent()
            {
            }

            [Test]
            public void DisposedCommittedObservationStageObserversDontRecievEvent()
            {
            }
        }

        public class ErrorFlows
        {
            [Test]
            public void CancelingTwiceAtPreviewObservationStageThrows()
            {
            }

            [Test]
            public void CancelingAtNormalObservationStageThrows()
            {
            }

            [Test]
            public void CancelingAtCommittedObservationStageThrows()
            {
            }

            [Test]
            public void CommittingAtPreviewObservationStageThrows()
            { }

            [Test]
            public void CommittingAtCommittedObservationStageThrows()
            { }

            [Test]
            public void CommittingTwiceAtNormalObservationStageThrows()
            { }
        }

        public class ModelObservation
        {
            [Test]
            public void ThrowsIfModelIdGuidEmpty()
            {
            }

            [Test]
            public void ObserversReceiveModelOnEventWorkflowCompleted()
            {
                
            }

            [Test]
            public void DisposedObserversReceiveDontModelOnEventWorkflowCompleted()
            {

            }

            [Test]
            public void MutipleSubsequentEventsOnlyYield1ModelUpdate()
            {
            }

            [Test]
            public void EventsPublishedDuringModelDispatchGetProcessedFromBackingQueue()
            {

            }

            public class ModelCloning
            {
                [Test]
                public void DispatchesAModelCloneIfTheModelImplementsIClonable()
                {
                }
            }
        }

        public class ModelRouter
        {
        }

        public class ThreadGuard
        {
            [Test]
            public void ShouldThrowIfRegisterModelCalledOnInvalidThread()
            {
                // all overloads
            }

            [Test]
            public void ShouldThrowIfRemoveModelCalledOnInvalidThread()
            {
            }

            [Test]
            public void ShouldThrowIfPublishEventCalledOnInvalidThread()
            {
            }

            [Test]
            public void ShouldThrowIfGetModelObservableCalledOnInvalidThread()
            {
            }

            [Test]
            public void ShouldThrowIfGetEventObservableCalledOnInvalidThread()
            {
                // all overloads
            }
        }

        public class RouterHalting
        {
            [Test]
            public void ShouldHaltAndRethrowIfAPreProcessorErrors()
            {
            }

            [Test]
            public void ShouldHaltAndRethrowIfAnEventProcessorErrors()
            {
            }

            [Test]
            public void ShouldHaltAndRethrowIfAnPostProcessorErrors()
            {
            }

            [Test]
            public void ShouldHaltAndRethrowIfAModelObserverErrors()
            {
            }

            public class WhenHalted
            {
                [Test]
                public void ShouldThrowOnRegisterModel()
                {
                }

                [Test]
                public void ShouldThrowOnCreateModelRouter()
                {
                }

                [Test]
                public void ShouldThrowOnPublish()
                {
                }

                [Test]
                public void ShouldThrowOnGetModelObservable()
                {
                }

                [Test]
                public void ShouldThrowOnGetEventObservable()
                {
                }

                [Test]
                public void ShouldThrowOnPublishEvent()
                {
                }

                [Test]
                public void ShouldThrowOnExecuteEvent()
                {
                }

                [Test]
                public void ShouldThrowOnBroadcastEvent()
                {
                }
            }
        }

        protected void PublishEventWithMultipeSubsequentEvents(int numberOfSubsequentEvents)
        {
            _router
                .GetEventObservable<TestModel, int>(_model1.Id)
                .Observe(
                    (model, @event) =>
                    {
                        for (int i = 0; i < numberOfSubsequentEvents; i++)
                        {
                            _router.PublishEvent(_model1.Id, new Event1());
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

            public bool ShouldRemoveOnModelObservation { get; set; }
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

        public class TestModelEventProcessor
        {
            private readonly Guid _modelId;
            private readonly int _id;
            private readonly IRouter _router;
            private readonly IList<ModelChangedEvent<TestModel>> _modelChangedEvents = new List<ModelChangedEvent<TestModel>>();

            public TestModelEventProcessor(IRouter router, Guid modelId, int id)
            {
                _modelId = modelId;
                _id = id;
                _router = router;
                
                Event1Details = ObserveEvent<Event1>();
                Event2Details = ObserveEvent<Event2>();
                Event3Details = ObserveEvent<Event3>();
                ListenForModelChangedEvents();
            }

            private void ListenForModelChangedEvents()
            {
                // This should never yield a ModelChangedEvent<T> for changes to TestModel with id '_modelId'.
                // You don't get change events for your own model else we'd end up in an infinite loop.
                // Thus this is observing changes to 'TestModel' other than '_modelId' with the intent of 
                // applying thoes changes to TestModel with id _modelId.
                _router
                    .GetEventObservable<TestModel, ModelChangedEvent<TestModel>>(_modelId)
                    // typically a filter would be used here to narrow down model change to those related to model with id _modelId
                    // .Where((m, e) => e.ModelId == someOtherDependentModelId)
                    .Observe(
                    (m, e, c) =>
                    {
                        // m in this case should have id of _modelId (like any other event), and e
                        // will have the model chagned event containing state of other TestModels that 
                        // have changed.
                        ModelChangedEvents.Add(e);
                    }
                );
            }

            public EventObservationDetails<Event1> Event1Details { get; private set; }
            public EventObservationDetails<Event2> Event2Details { get; private set; }
            public EventObservationDetails<Event3> Event3Details { get; private set; }

            public IList<ModelChangedEvent<TestModel>> ModelChangedEvents
            {
                get { return _modelChangedEvents; }
            }

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

            private ObservationStageDetails<TEvent> WireUpObservationStage<TEvent>(ObservationStage stage) where TEvent : BaseEvent
            {
                var details = new ObservationStageDetails<TEvent>(stage);
                details.ObservationDisposable = _router
                    .GetEventObservable<TestModel, TEvent>(_modelId, details.Stage)
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
                            model.ControllerShouldRemove = @event.ShouldRemoveOnModelObservation;
                            foreach (Action<TestModel, TEvent> action in details.Actions)
                            {
                                action(model, @event);
                            }
                        },
                        () =>
                        {
                            details.StreamCompletedCount++;
                        }
                    );
                return details;
            }

            public class EventObservationDetails<TEvent>
            {
                public ObservationStageDetails<TEvent> PreviewStage { get; set; }
                public ObservationStageDetails<TEvent> NormalStage { get; set; }
                public ObservationStageDetails<TEvent> CommittedStage { get; set; }
            }

            public class ObservationStageDetails<TEvent>
            {
                private readonly List<Action<TestModel, TEvent>> _actions;
                public ObservationStageDetails(ObservationStage stage)
                {
                    Stage = stage;
                    ReceivedEvents = new List<TEvent>();
                    _actions = new List<Action<TestModel, TEvent>>();
                    Actions = new ReadOnlyCollection<Action<TestModel, TEvent>>(_actions);
                }
                public ObservationStage Stage { get; private set; }
                public List<TEvent> ReceivedEvents { get; private set; }
                public IDisposable ObservationDisposable { get; set; }
                public int StreamCompletedCount { get; set; }
                public IList<Action<TestModel, TEvent>> Actions { get; private set; }
                public void RegisterAction(Action<TestModel, TEvent> action)
                {
                    _actions.Add(action);
                }
            }
        }

        public class TestController
        {
            public TestController(IRouter router, Guid modelId)
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