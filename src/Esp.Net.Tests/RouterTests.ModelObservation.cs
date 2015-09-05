using System;
using System.Collections.Generic;
using NUnit.Framework;
using Shouldly;

namespace Esp.Net
{
    public partial class RouterTests
    {
        public class ModelObservation : RouterTests
        {
            [Test]
            public void ThrowsIfModelIdNull()
            {
                Assert.Throws<ArgumentNullException>(() => _router.GetModelObservable<TestModel>(null));
            }

            [Test]
            public void ObserversReceiveModelOnEventWorkflowCompleted()
            {
                _router.PublishEvent(_model1.Id, new Event1());
                _model1Controller.ReceivedModels.Count.ShouldBe(1);
            }

            [Test]
            public void DisposedObserversReceiveDontModelOnEventWorkflowCompleted()
            {
                _model1Controller.ModelObservationDisposable.Dispose();
                _router.PublishEvent(_model1.Id, new Event1());
                _model1Controller.ReceivedModels.Count.ShouldBe(0);
            }

            [Test]
            public void MutipleSubsequentEventsOnlyYield1ModelUpdate()
            {
                PublishEventWithMultipeSubsequentEvents(5);
                _model1EventProcessor.Event1Details.NormalStage.ReceivedEvents.Count.ShouldBe(6);
                _model1Controller.ReceivedModels.Count.ShouldBe(1);
            }

            [Test]
            public void EventsPublishedDuringModelDispatchGetProcessed()
            {
                bool publishedEventFromController = false;
                _model1Controller.RegisterAction(m =>
                {
                    if (!publishedEventFromController)
                    {
                        publishedEventFromController = true;
                        _router.PublishEvent(_model1.Id, new Event1());
                    }
                });
                _router.PublishEvent(_model1.Id, new Event1());
                publishedEventFromController.ShouldBe(true);
                _model1Controller.ReceivedModels.Count.ShouldBe(2);
            }

            public class ModelCloning : RouterTests
            {
                [Test]
                public void DispatchesAModelCloneIfTheModelImplementsIClonable()
                {
                    var receivedModels = new List<TestModel4>();
                    _router.GetEventObservable<TestModel4, int>(_model4.Id).Observe((m, e) => { /*noop*/ });
                    _router.GetModelObservable<TestModel4>(_model4.Id).Observe(m => receivedModels.Add(m));
                    _router.PublishEvent(_model4.Id, 2);
                    _router.PublishEvent(_model4.Id, 4);
                    receivedModels.Count.ShouldBe(2);
                    receivedModels[0].IsClone.ShouldBe(true);
                    receivedModels[1].IsClone.ShouldBe(true);
                }
            }
        }
    }
}