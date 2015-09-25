using System;
using System.Collections.Generic;
using NUnit.Framework;
using Shouldly;

namespace Esp.Net
{
    public partial class RouterTests
    {
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

            [Test]
            public void TerminalErrorHandlerGetInvokedOnHaltingException()
            {
                var exception = new Exception("Boom");
                _model1Controller.RegisterAction(m =>
                {
                    throw exception;
                });
                AssertPublishEventThrows();
                _terminalErrorHandler.Errors.Count.ShouldBe(1);
                _terminalErrorHandler.Errors[0].ShouldBe(exception);
            }

            private void AssertPublishEventThrows()
            {
                _router.PublishEvent(_model1.Id, new Event1());
                _terminalErrorHandler.Errors.Count.ShouldBe(1);
                _terminalErrorHandler.Errors[0].ShouldBeOfType<Exception>();
                _terminalErrorHandler.Errors[0].Message.ShouldBe("Boom");

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
                public void ShouldThrowOnAddModel()
                {
                    AssertRethrows(() => _router.AddModel(Guid.NewGuid(), _model1));
                    AssertRethrows(() => _router.AddModel(Guid.NewGuid(), _model1, (IPreEventProcessor<TestModel>)new StubModelProcessor()));
                    AssertRethrows(() => _router.AddModel(Guid.NewGuid(), _model1, (IPostEventProcessor<TestModel>)new StubModelProcessor()));
                    AssertRethrows(() => _router.AddModel(Guid.NewGuid(), _model1, new StubModelProcessor(), new StubModelProcessor()));
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
    }
}