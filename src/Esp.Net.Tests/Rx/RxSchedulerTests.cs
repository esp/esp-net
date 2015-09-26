using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Microsoft.Reactive.Testing;
using NUnit.Framework;
using Shouldly;

namespace Esp.Net.Rx
{
    [TestFixture]
    public class RxSchedulerTests
    {

        private Router<TestModel> _router;
        private TestModel _model;
        private TestScheduler _testScheduler;

        [SetUp]
        public void SetUp()
        {
            // need to solve this chicken and egg problem : creating the router with a model which takes a router
            // might have to make the router take a factory

            _testScheduler = new TestScheduler();
            var router = new Router();
            _router = new Router<TestModel>("modelId", router);
            _model = new TestModel(_router, _testScheduler);
            router.AddModel("modelId", _model);
        }

        [Test]
        public void ObserverInvokedOnRouterDispatchLoop()
        {
            _model.ObserveEvents();
            bool modelUpdated = false;
            _router.GetModelObservable().Observe(m =>
            {
                modelUpdated = m.ReceivedInts.Count == 1 && m.Version == 2;
            });
            _router.PublishEvent(new InitialEvent());
            _testScheduler.AdvanceBy(1);
            _model.ReceivedInts.Count.ShouldBe(1);
            _model.ReceivedInts[0].ShouldBe(0);
            modelUpdated.ShouldBe(true);
        }

        public class TestModel : Esp.Net.DisposableBase, IPreEventProcessor
        {
            private readonly IRouter<TestModel> _rouer;
            private readonly IScheduler _rxScheduler;
            private readonly RouterScheduler<TestModel> _routerScheduler;

            public TestModel(IRouter<TestModel> rouer, IScheduler rxScheduler)
            {
                _rouer = rouer;
                _rxScheduler = rxScheduler;
                ReceivedInts = new List<long>();
                Id = Guid.NewGuid();
                _routerScheduler = new RouterScheduler<TestModel>(rouer);
            }

            public void ObserveEvents()
            {
                _rouer.ObserveEventsOn(this);
            }

            public Guid Id { get; private set; }

            public int Version { get; private set; }

            public List<long> ReceivedInts { get; set; }

            [ObserveEvent(typeof(InitialEvent))]
            private void ObserveIntEvent()
            {
                AddDisposable(
                    Observable
                        .Timer(TimeSpan.FromTicks(1), _rxScheduler)
                        .ObserveOn(_routerScheduler)
                        .Subscribe(i =>
                        {
                            ReceivedInts.Add(i);
                            // on the dispatch loop, updating private state is ok 
                        }
                    )
                );
            }

            void IPreEventProcessor.Process()
            {
                Version++;
            }
        }

        public class InitialEvent { }

    }
}
