using System;
using NUnit.Framework;
using Shouldly;

namespace Esp.Net
{
    public partial class RouterTests
    {
        public class ModelRouterCtor : RouterTests
        {
            [SetUp]
            public override void SetUp()
            {
                
            }

            [Test]
            public void CanOnlySetModelOnce()
            {
                var router = new Router<TestModel>();
                router.SetModel(new TestModel());
                Assert.Throws<InvalidOperationException>(() => { router.SetModel(new TestModel()); });
            }

            [Test]
            public void ThrowsIfModelNotSet()
            {
                var router = new Router<TestModel>();
                var ex = Assert.Throws<InvalidOperationException>(() => { router.PublishEvent("SomeEvent"); });
                ex.Message.ShouldContain("Model not set. You must call ruter.SetModel(model) passing the model.");
            }
        }

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
            public void RouterCreateModelRouterRetunsModelRouter()
            {
                var model1 = new TestModel();
                var receivedEventCount = 0;
                _router.AddModel(model1.Id, model1);
                IRouter<TestModel> modelRouter = _router.CreateModelRouter<TestModel>(model1.Id);
                modelRouter.GetEventObservable<Event1>().Observe((m, e, c) => receivedEventCount++);
                modelRouter.PublishEvent(new Event1());
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