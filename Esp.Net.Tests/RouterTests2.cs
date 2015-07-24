using NUnit.Framework;

namespace Esp.Net
{
    [TestFixture]
    public class RouterTests2
    {
        public class RegisterModel
        {
            [Test, Ignore]
            public void ThrowsIfModelIdNull()
            {
            }

            [Test, Ignore]
            public void ThrowsIfPreEventProcessorNull()
            {
            }

            [Test, Ignore]
            public void ThrowsIfPostEventProcessorNull()
            {
            }

            [Test, Ignore]
            public void ThrowsIfPreAndPostEventProcessorNull()
            {
            }

            [Test, Ignore]
            public void ThrowsIfModelAlreadyRegistered()
            {
            }

            [Test, Ignore]
            public void ThrowsIfModelNull()
            {
            }
        }

        public class RemoveModel
        {
            [Test, Ignore]
            public void EventObserversCompleteOnRemoval()
            {
            }

            [Test, Ignore]
            public void ModelObserversCompleteOnRemoval()
            {
            }

            [Test, Ignore]
            public void QueuedEventsAreIgnoredOnRemoval()
            {
            }

            [Test, Ignore]
            public void RemovalByAPreProcessorEndsEventWorkflow()
            {
            }

            [Test, Ignore]
            public void RemovalAtPreviewStageEndsEventWorkflow()
            {
            }

            [Test, Ignore]
            public void RemovalAtNormalStageEndsEventWorkflow()
            {
            }

            [Test, Ignore]
            public void RemovalAtCommittedStageEndsEventWorkflow()
            {
            }

            [Test, Ignore]
            public void RemovalByAPostProcessorEndsEventWorkflow()
            {
            }

            [Test, Ignore]
            public void RemovalByAModelObserverEndsEventWorkflow()
            {
            }
        }

        public class EventWorkflow
        {
            [Test, Ignore]
            public void PreProcessorInvokedForFirstEvent()
            {
            }

            [Test, Ignore]
            public void PreviewObservationStageObserversRecievEvent()
            {
                // note: multiple observers 
            }

            [Test, Ignore]
            public void NormalObservationStageObserversRecieveEvent()
            {
            }

            [Test, Ignore]
            public void CommittedObservationStageObserversRecieveEvent()
            {
            }

            [Test, Ignore]
            public void PostProcessorInvokedAfterAllEventsPurged()
            {

            }

            [Test, Ignore]
            public void EventSentToPreProcessorThenEventProcessorThenPostProcessors()
            {
            }

            [Test, Ignore]
            public void EventWorkflowOnlyInvokedForTargetModel()
            {
            }

            public class SubsequentEvents
            {
                [Test, Ignore]
                public void EventsPublishedByPreProcessorGetProcessedFromBackingQueue()
                {
                }

                [Test, Ignore]
                public void EventsPublishedByPreviewObservationStageObserversGetProcessedFromBackingQueue()
                {
                }

                [Test, Ignore]
                public void EventsPublishedByNormalObservationStageObserversGetProcessedFromBackingQueue()
                {
                }

                [Test, Ignore]
                public void EventsPublishedByCommittedObservationStageObserversGetProcessedFromBackingQueue()
                {
                }

                [Test, Ignore]
                public void EventsPublishedByPostProcessorGetProcessedFromBackingQueue()
                {
                }
            }

            public class ExecuteEvent
            {
                [Test, Ignore]
                public void OnlyAllowsExecuteDuringEventWorkflowStages()
                {
                }

                [Test, Ignore]
                public void ThrowsIfExecuteHandlerRaisesAnotherEvent()
                {
                }

                [Test, Ignore]
                public void ImmediatelyPublishesTheExecutedEventToPreviewObservationStageObservers()
                {
                }

                [Test, Ignore]
                public void ImmediatelyPublishesTheExecutedEventToNormalObservationStageObservers()
                {
                }

                [Test, Ignore]
                public void ImmediatelyPublishesTheExecutedEventToCommittedObservationStageObservers()
                {
                }
            }

            public class Broadcast
            {
                [Test, Ignore]
                public void DeliversEventToAllModels()
                { }
            }

            public class ModelChangedEvent
            {
                [Test, Ignore]
                public void ThrowsIfEventProcessorSubscribesToOwnModelChangedEvent()
                {
                }

                [Test, Ignore]
                public void WhenEventProcessingWorkflowFinishedModelChangedEventIsRaised()
                {
                }
            }

            public class EventObservationDisposal
            {
                [Test, Ignore]
                public void DisposedPreviewObservationStageObserversDontRecievEvent()
                {
                }

                [Test, Ignore]
                public void DisposedNormalObservationStageObserversDontRecievEvent()
                {
                }

                [Test, Ignore]
                public void DisposedCommittedObservationStageObserversDontRecievEvent()
                {
                }
            }

            public class ErrorFlows
            {
                [Test, Ignore]
                public void CancelingTwiceAtPreviewObservationStageThrows()
                {
                }

                [Test, Ignore]
                public void CancelingAtNormalObservationStageThrows()
                {
                }

                [Test, Ignore]
                public void CancelingAtCommittedObservationStageThrows()
                {
                }

                [Test, Ignore]
                public void CommittingAtPreviewObservationStageThrows()
                { }

                [Test, Ignore]
                public void CommittingAtCommittedObservationStageThrows()
                { }

                [Test, Ignore]
                public void CommittingTwiceAtNormalObservationStageThrows()
                { }
            }

            // pre
            // staged
            // post
        }

        public class ModelObservation
        {
            [Test, Ignore]
            public void ThrowsIfModelIdGuidEmpty()
            {
            }

            [Test, Ignore]
            public void ObserversReceiveModelOnEventWorkflowCompleted()
            {
                
            }

            [Test, Ignore]
            public void DisposedObserversReceiveDontModelOnEventWorkflowCompleted()
            {

            }

            [Test, Ignore]
            public void MutipleSubsequentEventsOnlyYield1ModelUpdate()
            {
            }

            [Test, Ignore]
            public void EventsPublishedDuringModelDispatchGetProcessedFromBackingQueue()
            {

            }

            public class ModelCloning
            {
                [Test, Ignore]
                public void DispatchesAModelCloneIfTheModelImplementsIClonable()
                {
                }
            }
        }

        public class ModelRouter
        {
        }

        public class RouterHalting
        {
            [Test, Ignore]
            public void ShouldHaltAndRethrowIfAPreProcessorErrors()
            {
            }

            [Test, Ignore]
            public void ShouldHaltAndRethrowIfAnEventProcessorErrors()
            {
            }

            [Test, Ignore]
            public void ShouldHaltAndRethrowIfAnPostProcessorErrors()
            {
            }

            [Test, Ignore]
            public void ShouldHaltAndRethrowIfAModelObserverErrors()
            {
            }

            public class WhenHalted
            {
                [Test, Ignore]
                public void ShouldThrowOnRegisterModel()
                {
                }

                [Test, Ignore]
                public void ShouldThrowOnCreateModelRouter()
                {
                }

                [Test, Ignore]
                public void ShouldThrowOnPublish()
                {
                }

                [Test, Ignore]
                public void ShouldThrowOnGetModelObservable()
                {
                }

                [Test, Ignore]
                public void ShouldThrowOnGetEventObservable()
                {
                }

                [Test, Ignore]
                public void ShouldThrowOnPublishEvent()
                {
                }

                [Test, Ignore]
                public void ShouldThrowOnExecuteEvent()
                {
                }

                [Test, Ignore]
                public void ShouldThrowOnBroadcastEvent()
                {
                }
            }
        }
    }
}