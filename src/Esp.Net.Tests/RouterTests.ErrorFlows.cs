using System;
using NUnit.Framework;
using Shouldly;

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

            [Test]
            public void ObservingAPrivateEventThrowsAnInvalidOperationException()
            {
                var ex = Assert.Throws<Exception>(() =>
                {
                    _router.GetEventObservable<TestModel, PrivateBaseEvent>(_model1.Id, typeof(PrivateEvent)).Observe((model, ev) => { });
                });
                ex.Message.ShouldContain("Is this event scoped as private or internal");
            }

            [Test]
            public void PublishingAPrivateEventThrowsAnInvalidOperationException()
            {
                _router.GetEventObservable<TestModel, PrivateEvent>(_model1.Id).Observe((model, ev) => { });
                var ex = Assert.Throws<Exception>(() => _router.PublishEvent(_model1.Id, new PrivateEvent()));
                ex.Message.ShouldContain("Is this event scoped as private or internal");
            }

            private class PrivateEvent : PrivateBaseEvent { }
            private class PrivateBaseEvent { }
        }
    }
}