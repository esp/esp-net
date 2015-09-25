using System;
using System.Collections.Generic;
using NUnit.Framework;
using Shouldly;

namespace Esp.Net
{
    public partial class RouterTests
    {
        public class ModelSubRouter : RouterTests
        {
            private IRouter<SubTestModel> _modelRouter;

            [SetUp]
            public override void SetUp()
            {
                base.SetUp();
                _modelRouter = new Router<TestModel, SubTestModel>(_model1.Id, _router, m => m.SubTestModel);
            }

            [Test]
            public void CanPublishAndObserveProxiedEvent()
            {
                List<Tuple<SubTestModel, Event1>> receivedSubModels = new List<Tuple<SubTestModel, Event1>>();
                _modelRouter.GetEventObservable<Event1>().Observe((m, e, c) => receivedSubModels.Add(Tuple.Create(m, e)));
                _modelRouter.PublishEvent(new Event1());
                receivedSubModels.Count.ShouldBe(1);
                receivedSubModels[0].Item1.ShouldBe(_model1.SubTestModel);
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