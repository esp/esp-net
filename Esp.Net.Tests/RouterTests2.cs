using System;
using System.Collections.Generic;
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
        private TestController _model1Controller;

        private TestModel _model2;
        private StubModelProcessor _model2PreEventProcessor;
        private StubModelProcessor _model2PostEventProcessor;
        private TestModelEventProcessor _model2EventProcessor;
        private TestController _model2Controller;
        
        [SetUp]
        public void SetUp()
        {
            _threadGuard = new StubThreadGuard();
            _router = new Router(_threadGuard);

            _model1 = new TestModel();
            _model1PreEventProcessor = new StubModelProcessor();
            _model1PostEventProcessor = new StubModelProcessor();
            _router.RegisterModel(_model1.Id, _model1, _model1PreEventProcessor, _model1PostEventProcessor);
            _model1EventProcessor = new TestModelEventProcessor(_router, _model1.Id);
            _model1Controller = new TestController(_router, _model1.Id);

            _model2 = new TestModel();
            _model2PreEventProcessor = new StubModelProcessor();
            _model2PostEventProcessor = new StubModelProcessor();
            _router.RegisterModel(_model2.Id, _model2, _model2PreEventProcessor, _model2PostEventProcessor);
            _model2EventProcessor = new TestModelEventProcessor(_router, _model2.Id);
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
                _router
                    .GetEventObservable<TestModel, int>(_model1.Id)
                    .Observe(
                        (model, @event) =>
                        {
                            _router.PublishEvent(_model1.Id, new Event1());
                            _router.RemoveModel(_model1.Id);
                        }
                    );
                _router.PublishEvent(_model1.Id, 1);
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
                _router.PublishEvent(_model1.Id, new Event1(){ ShouldRemove = true, RemoveAtAtStage = ObservationStage.Preview});
                _model1EventProcessor.Event1Details.PreviewStage.ReceivedEvents.Count.ShouldBe(1);
                _model1EventProcessor.Event1Details.NormalStage.ReceivedEvents.Count.ShouldBe(0);
                _model1EventProcessor.Event1Details.CommittedStage.ReceivedEvents.Count.ShouldBe(0);
                _model1Controller.ReceivedModels.Count.ShouldBe(0);
            }

            [Test]
            public void RemovalAtNormalStageEndsEventWorkflow()
            {
                _router.PublishEvent(_model1.Id, new Event1() { ShouldRemove = true, RemoveAtAtStage = ObservationStage.Normal, ShouldCommit = true });
                _model1EventProcessor.Event1Details.PreviewStage.ReceivedEvents.Count.ShouldBe(1);
                _model1EventProcessor.Event1Details.NormalStage.ReceivedEvents.Count.ShouldBe(1);
                _model1EventProcessor.Event1Details.CommittedStage.ReceivedEvents.Count.ShouldBe(0);
                _model1Controller.ReceivedModels.Count.ShouldBe(0);
            }

            [Test]
            public void RemovalAtCommittedStageEndsEventWorkflow()
            {
                _router.PublishEvent(_model1.Id, new Event1() { ShouldRemove = true, RemoveAtAtStage = ObservationStage.Committed, ShouldCommit = true });
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

        public class EventWorkflow
        {
            [Test]
            public void PreProcessorInvokedForFirstEvent()
            {
            }

            [Test]
            public void PreviewObservationStageObserversRecievEvent()
            {
                // note: multiple observers 
            }

            [Test]
            public void NormalObservationStageObserversRecieveEvent()
            {
            }

            [Test]
            public void CommittedObservationStageObserversRecieveEvent()
            {
            }

            [Test]
            public void PostProcessorInvokedAfterAllEventsPurged()
            {

            }

            [Test]
            public void EventSentToPreProcessorThenEventProcessorThenPostProcessors()
            {
            }

            [Test]
            public void EventWorkflowOnlyInvokedForTargetModel()
            {
            }

            public class SubsequentEvents
            {
                [Test]
                public void EventsPublishedByPreProcessorGetProcessedFromBackingQueue()
                {
                }

                [Test]
                public void EventsPublishedByPreviewObservationStageObserversGetProcessedFromBackingQueue()
                {
                }

                [Test]
                public void EventsPublishedByNormalObservationStageObserversGetProcessedFromBackingQueue()
                {
                }

                [Test]
                public void EventsPublishedByCommittedObservationStageObserversGetProcessedFromBackingQueue()
                {
                }

                [Test]
                public void EventsPublishedByPostProcessorGetProcessedFromBackingQueue()
                {
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

            public class ModelChangedEvent
            {
                [Test]
                public void ThrowsIfEventProcessorSubscribesToOwnModelChangedEvent()
                {
                }

                [Test]
                public void WhenEventProcessingWorkflowFinishedModelChangedEventIsRaised()
                {
                }
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

            // pre
            // staged
            // post
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
            public bool ShouldCommit { get; set; }
            public bool ShouldRemove { get; set; }
            public bool ShouldRemoveOnModelObservation { get; set; }
            public ObservationStage CancelAtStage { get; set; }
            public ObservationStage RemoveAtAtStage { get; set; }
            public ObservationStage CommitAtAtStage { get; set; }
        }

        public class Event1 : BaseEvent { }
        public class Event2 : BaseEvent { }
        public class Event3 : BaseEvent { }

        public class StubModelProcessor : IPreEventProcessor<TestModel>, IPostEventProcessor<TestModel>
        {
            private readonly List<Action<TestModel>> _actions = new List<Action<TestModel>>();

            public void Process(TestModel model)
            {
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
            private readonly IRouter _router;

            public TestModelEventProcessor(IRouter router, Guid modelId)
            {
                _modelId = modelId;
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

            private ObservationStageDetails<TEvent> WireUpObservationStage<TEvent>(ObservationStage stage) where TEvent : BaseEvent
            {
                var details = new ObservationStageDetails<TEvent>(stage);
                details.ObservationDisposable = _router
                    .GetEventObservable<TestModel, TEvent>(_modelId, details.Stage)
                    .Observe(
                        (model, @event, context) =>
                        {
                            details.ReceivedEvents.Add(@event);
                            var shouldCancel = @event.ShouldCancel && stage == @event.CancelAtStage;
                            if (shouldCancel)
                            {
                                context.Cancel();
                            }
                            var shouldCommit = @event.ShouldCommit && stage == @event.CommitAtAtStage;
                            if (shouldCommit)
                            {
                                context.Commit();
                            }
                            var shouldRemove = @event.ShouldRemove && stage == @event.RemoveAtAtStage;
                            if (shouldRemove)
                            {
                                _router.RemoveModel(_modelId);
                            }
                            model.ControllerShouldRemove = @event.ShouldRemoveOnModelObservation;
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
                public ObservationStageDetails(ObservationStage stage)
                {
                    Stage = stage;
                    ReceivedEvents = new List<TEvent>();
                }

                public ObservationStage Stage { get; private set; }
                public List<TEvent> ReceivedEvents { get; private set; }
                public IDisposable ObservationDisposable { get; set; }
                public int StreamCompletedCount { get; set; }
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