using System;
using NUnit.Framework;

namespace Esp.Net
{
    public partial class RouterTests
    {
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
    }
}