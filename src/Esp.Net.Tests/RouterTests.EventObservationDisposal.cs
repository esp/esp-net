using System;
using NUnit.Framework;
using Shouldly;

namespace Esp.Net
{
    public partial class RouterTests
    {
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
                _router.PublishEvent(_model1.Id, new Event1 { ShouldCommit = true, CommitAtStage = ObservationStage.Normal, CommitAtEventProcesserId = EventProcessor1Id });
                _model1EventProcessor.Event1Details.NormalStage.ReceivedEvents.Count.ShouldBe(1);
                _model1EventProcessor.Event1Details.CommittedStage.ReceivedEvents.Count.ShouldBe(0);
            }
        }
    }
}