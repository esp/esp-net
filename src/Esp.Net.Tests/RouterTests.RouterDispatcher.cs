using System;
using NUnit.Framework;
using Shouldly;

namespace Esp.Net
{
    public partial class RouterTests
    {
        public class RouterDispatcher : RouterTests
        {
            [SetUp]
            public override void SetUp()
            {
                base.SetUp();
                _routerDispatcher.HasAccess = false;
            }

            [Test]
            public void ShouldDispatchRemoveCallViaDispatcher()
            {
                _router.RemoveModel(_model1.Id);
                _model1EventProcessor.Event1Details.NormalStage.StreamCompletedCount.ShouldBe(0);
                _routerDispatcher.InvokeDispatchedActions(1);
                _model1EventProcessor.Event1Details.NormalStage.StreamCompletedCount.ShouldBe(1);
            }

            [Test]
            public void ShouldDispatchPublishCallViaDispatcher()
            {
                _router.PublishEvent(_model1.Id, new Event1());
                _model1EventProcessor.Event1Details.NormalStage.ReceivedEvents.Count.ShouldBe(0);
                _routerDispatcher.InvokeDispatchedActions(1);
                _model1EventProcessor.Event1Details.NormalStage.ReceivedEvents.Count.ShouldBe(1);
            }

            [Test]
            public void ShouldDispatchBroadcastEventCallViaDispatcher()
            {
                _router.BroadcastEvent(new Event1());
                _model1EventProcessor.Event1Details.NormalStage.ReceivedEvents.Count.ShouldBe(0);
                _routerDispatcher.InvokeDispatchedActions(1);
                _model1EventProcessor.Event1Details.NormalStage.ReceivedEvents.Count.ShouldBe(1);
            }

            [Test]
            public void ShouldThrowIfExecuteEventCalledOnInvalidThread()
            {
                Assert.Throws<InvalidOperationException>(() => _router.ExecuteEvent(_model1.Id, new Event1()));
            }
        }

        public class DefaultRouterDispatcher : RouterTests
        {
            [SetUp]
            public override void SetUp()
            {
                _router = new Router(); // default ctor

                _model1 = new TestModel();
                _router.RegisterModel(_model1.Id, _model1);
                _model1EventProcessor = new GenericModelEventProcessor<TestModel>(_router, _model1.Id, EventProcessor1Id);
                _model1Controller = new TestModelController(_router, _model1.Id);
            }

            [Test]
            public void ShouldDispatchRemoveOnCurrentThread()
            {
                _router.RemoveModel(_model1.Id);
                _model1EventProcessor.Event1Details.NormalStage.StreamCompletedCount.ShouldBe(1);
            }

            [Test]
            public void ShouldDispatchPublishCallOnCurrentThread()
            {
                _router.PublishEvent(_model1.Id, new Event1());
                _model1EventProcessor.Event1Details.NormalStage.ReceivedEvents.Count.ShouldBe(1);
            }

            [Test]
            public void ShouldDispatchBroadcastEventCallOnCurrentThread()
            {
                _router.BroadcastEvent(new Event1());
                _model1EventProcessor.Event1Details.NormalStage.ReceivedEvents.Count.ShouldBe(1);
            }
        }
    }
}