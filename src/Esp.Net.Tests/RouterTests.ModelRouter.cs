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
                _modelRouter = new Router<TestModel>(_model1.Id, _router);
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
        }
    }
}