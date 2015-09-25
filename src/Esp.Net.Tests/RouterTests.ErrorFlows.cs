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
                var event1 = new Event1
                {
                    ShouldCancel = true,
                    CancelAtStage = ObservationStage.Normal,
                    CancelAtEventProcesserId = EventProcessor1Id,
                };
                _router.PublishEvent(_model1.Id, event1);
                _terminalErrorHandler.Errors.Count.ShouldBe(1);
                _terminalErrorHandler.Errors[0].ShouldBeOfType<InvalidOperationException>();
            }

            [Test]
            public void CancelingAtCommittedObservationStageThrows()
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
                _terminalErrorHandler.Errors.Count.ShouldBe(1);
                _terminalErrorHandler.Errors[0].ShouldBeOfType<InvalidOperationException>();
            }

            [Test]
            public void CommittingAtPreviewObservationStageThrows()
            {
                var event1 = new Event1
                {
                    ShouldCommit = true,
                    CommitAtStage = ObservationStage.Preview,
                    CommitAtEventProcesserId = EventProcessor1Id,
                };
                _router.PublishEvent(_model1.Id, event1);
                _terminalErrorHandler.Errors.Count.ShouldBe(1);
                _terminalErrorHandler.Errors[0].ShouldBeOfType<InvalidOperationException>();
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
                _router.PublishEvent(_model1.Id, new PrivateEvent());
                _terminalErrorHandler.Errors.Count.ShouldBe(1);
                _terminalErrorHandler.Errors[0].ShouldBeOfType<Exception>();
                _terminalErrorHandler.Errors[0].Message.ShouldContain("Is this event scoped as private or internal");
            }

            [Test]
            public void RethrowsWhenNoTerminalErrorHandlerRegistered()
            {
                var router = new Router<TestModel>(new TestModel());
                router.GetEventObservable<Event1>().Observe((m, e) =>
                {
                    throw new InvalidOperationException("Boom");
                });
                Assert.Throws<InvalidOperationException>(() =>
                {
                    router.PublishEvent(new Event1());
                });
            }

            private class PrivateEvent : PrivateBaseEvent { }
            private class PrivateBaseEvent { }
        }
    }
}