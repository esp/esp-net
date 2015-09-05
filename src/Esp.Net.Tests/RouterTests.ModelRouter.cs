using System;
using NUnit.Framework;
using Shouldly;

namespace Esp.Net
{
    public partial class RouterTests
    {
        public class ModelRouter : RouterTests
        {
            private IRouter<TestModel> _modelRouter;

            [SetUp]
            public override void SetUp()
            {
                base.SetUp();
                _modelRouter = _router.CreateModelRouter<TestModel>(_model1.Id);
            }

            [Test]
            public void CanPublishAndObserveProxiedEvent()
            {
                var receivedEventCount = 0;
                _modelRouter.GetEventObservable<Event1>().Observe((m, e, c) => receivedEventCount++);
                _modelRouter.PublishEvent(new Event1());
                receivedEventCount.ShouldBe(1);
            }

            [Test]
            public void CanObserveProxiedModel()
            {
                var receivedModelCount = 0;
                _modelRouter.GetModelObservable().Observe(m => receivedModelCount++);
                _modelRouter.PublishEvent(new Event1());
                receivedModelCount.ShouldBe(1);
            }

            [Test]
            public void CanBroadcastProxiedEvent()
            {
                _modelRouter.BroadcastEvent(new Event1());
                _model1EventProcessor.Event1Details.NormalStage.ReceivedEvents.Count.ShouldBe(1);
                _model2EventProcessor.Event1Details.NormalStage.ReceivedEvents.Count.ShouldBe(1);
            }
        }
    }
}