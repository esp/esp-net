using System;
using System.Collections.Generic;
using NUnit.Framework;

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
        
        private TestModel _model2;
        private StubModelProcessor _model2PreEventProcessor;
        private StubModelProcessor _model2PostEventProcessor;
        
        [SetUp]
        public void SetUp()
        {
            _threadGuard = new StubThreadGuard();
            _router = new Router(_threadGuard);

            _model1 = new TestModel();
            _model1PreEventProcessor = new StubModelProcessor();
            _model1PostEventProcessor = new StubModelProcessor();
            _router.RegisterModel(_model1.Id, _model1, _model1PreEventProcessor, _model1PostEventProcessor);

            _model2 = new TestModel();
            _model2PreEventProcessor = new StubModelProcessor();
            _model2PostEventProcessor = new StubModelProcessor();
            _router.RegisterModel(_model2.Id, _model2, _model2PreEventProcessor, _model2PostEventProcessor);
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

        public class RemoveModel
        {
            [Test]
            public void EventObserversCompleteOnRemoval()
            {
            }

            [Test]
            public void ModelObserversCompleteOnRemoval()
            {
            }

            [Test]
            public void QueuedEventsAreIgnoredOnRemoval()
            {
            }

            [Test]
            public void RemovalByAPreProcessorEndsEventWorkflow()
            {
            }

            [Test]
            public void RemovalAtPreviewStageEndsEventWorkflow()
            {
            }

            [Test]
            public void RemovalAtNormalStageEndsEventWorkflow()
            {
            }

            [Test]
            public void RemovalAtCommittedStageEndsEventWorkflow()
            {
            }

            [Test]
            public void RemovalByAPostProcessorEndsEventWorkflow()
            {
            }

            [Test]
            public void RemovalByAModelObserverEndsEventWorkflow()
            {
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
            public int AnInt { get; set; }
            public string AString { get; set; }
            public decimal ADecimal { get; set; }
        }

        public class BaseEvent { }
        public class Event1 : BaseEvent { }
        public class Event2 : BaseEvent { }
        public class Event3 : BaseEvent { }
        public class EventWithoutBaseType { }

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

        public class StubThreadGuard : IThreadGuard
        {
            public bool HasAccess { get; set; }

            public bool CheckAccess()
            {
                return HasAccess;
            }
        }
    }
}