using System;
using NUnit.Framework;
using Shouldly;

namespace Esp.Net
{
    public partial class RouterTests
    {
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
    }
}