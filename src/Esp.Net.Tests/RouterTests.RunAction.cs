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
        public class RunAction
        {
            [Test]
            public void RunsAction()
            {
                int action1RunCount = 0, action2RunCount = 0;
                bool modelUpdated = false;
                var testScheduler = new  TestScheduler();
                var router = new Router<TestModel>(new TestModel());
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