using System;
using System.Reactive.Linq;
using System.Threading;
using Microsoft.Reactive.Testing;
using NUnit.Framework;
using Shouldly;

namespace Esp.Net
{
    public partial class RouterTests
    {
        public class ErrorRunActionFlows : RouterTests
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
            public void RunningAnActionPushesAModelUpdate()
            {
                bool wasRun = false;
                _router.RunAction(_model1.Id, () =>
                {
                    wasRun = true;
                });
                _model1Controller.ReceivedModels.Count.ShouldBe(1);
                _model2Controller.ReceivedModels.Count.ShouldBe(0);
                wasRun.ShouldBe(true);
            }

            [Test]
            public void RunningAnActionReceivesModel()
            {
                bool wasRun = false, modelCorrect = false;

                _router.RunAction<RouterTests.TestModel>(_model1.Id, model =>
                {
                    wasRun = true;
                    modelCorrect = model.Id == _model1.Id;
                });
                _model1Controller.ReceivedModels.Count.ShouldBe(1);
                _model2Controller.ReceivedModels.Count.ShouldBe(0);
                modelCorrect.ShouldBe(true);
            }

            [Test]
            public void RunningAnActionRunsAction()
            {
                int action1RunCount = 0, action2RunCount = 0;
                bool modelUpdated = false;
                var testScheduler = new  TestScheduler();

                var router = new Router<TestModel>();
                router.SetModel(new TestModel());

                router.GetEventObservable<int>().Observe((m, e) =>
                {
                    var observable = Observable.Timer(TimeSpan.FromSeconds(1), testScheduler);
                    observable.Subscribe(i =>
                    {
                        router.RunAction(() =>
                        {
                            action1RunCount++;
                        });
                        router.RunAction((model) =>
                        {
                            action2RunCount++;
                            model.Count++;
                        });
                    });
                });
                router.GetModelObservable().Observe(m =>
                {
                    Console.WriteLine(m.Count);
                    modelUpdated = m.Count == 1 && m.Version == 3;
                });
                router.PublishEvent(1);
                testScheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);
                action1RunCount.ShouldBe(1);
                action2RunCount.ShouldBe(1);
                modelUpdated.ShouldBe(true);
            }

            public class TestModel : IPreEventProcessor
            {
                public int Version { get; set; }

                public int Count { get; set; }

                public void Process()
                {
                    Version++;
                }
            }
        }
    }
}