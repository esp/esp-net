using System;
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

            private void AssertPublishEventThrows()
            {
                Exception ex = Assert.Throws<Exception>(() => _router.PublishEvent(_model1.Id, new Event1()));
                ex.Message.ShouldBe("Boom");
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
                public void ShouldThrowOnRegisterModel()
                {
                    AssertRethrows(() => _router.RegisterModel(Guid.NewGuid(), _model1));
                    AssertRethrows(() => _router.RegisterModel(Guid.NewGuid(), _model1, (IPreEventProcessor<TestModel>)new StubModelProcessor()));
                    AssertRethrows(() => _router.RegisterModel(Guid.NewGuid(), _model1, (IPostEventProcessor<TestModel>)new StubModelProcessor()));
                    AssertRethrows(() => _router.RegisterModel(Guid.NewGuid(), _model1, new StubModelProcessor(), new StubModelProcessor()));
                }

                [Test]
                public void ShouldThrowOnCreateModelRouter()
                {
                    AssertRethrows(() => _router.CreateModelRouter<TestModel>(_model1.Id));
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