using System;
using System.Reactive.Linq;
using System.Threading;
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
                var gate = new ManualResetEvent(false);
                var router = new Router<TestModel>(new TestModel(), new NewThreadRouterDispatcher());
                router.GetEventObservable<int>().Observe((m, e) =>
                {
                    Observable.Timer(TimeSpan.FromSeconds(1)).Subscribe(i =>
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
                    if(m.Count == 2) gate.Set();
                });
                router.PublishEvent(1);
                gate.WaitOne(3000);
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