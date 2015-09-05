using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Shouldly;

namespace Esp.Net.Dispatchers
{
    [TestFixture]
    public class NewThreadRouterDispatcherTests
    {
        private NewThreadRouterDispatcher _dispatcher;

        [SetUp]
        public void SetUp()
        {
            _dispatcher = new NewThreadRouterDispatcher(ts => new Thread(ts));
        }

        [Test]
        public void CheckAccessReturnsFalseOnOtherThread()
        {
            _dispatcher.CheckAccess().ShouldBe(false);
        }

        [Test]
        public void CheckAccessReturnsTrueOnDispatcherThread()
        {
            ManualResetEvent gate = new ManualResetEvent(false);
            bool? checkAccess = null;
            _dispatcher.Dispatch(() =>
            {
                checkAccess = _dispatcher.CheckAccess();
                gate.Set();
            });
            gate.WaitOne(100);
            checkAccess.ShouldNotBe(null);
            checkAccess.Value.ShouldBe(true);
        }

        [Test]
        public void EnsureAccessThrowsOnOtherThread()
        {
            var exception = Assert.Throws<InvalidOperationException>(() => _dispatcher.EnsureAccess());
            exception.Message.ShouldBe("Router accessed on invalid thread");
        }

        [Test]
        public void DispatchThrowsIfActionNull()
        {
            Assert.Throws<ArgumentNullException>(() => _dispatcher.Dispatch(null));
        }

        [Test]
        public void DispatchThrowsIfDisposed()
        {
            _dispatcher.Dispose();
            Assert.Throws<ObjectDisposedException>(() => _dispatcher.Dispatch(() => { }));
        }

        [Test]
        public void DispatchRunsAction()
        {
            ManualResetEvent gate = new ManualResetEvent(false);
            bool? wasRun = null;
            _dispatcher.Dispatch(() =>
            {
                wasRun = true;
                gate.Set();
            });
            gate.WaitOne(100);
            wasRun.ShouldNotBe(null);
            wasRun.Value.ShouldBe(true);
        }

        [Test]
        public void DispatchQueuesSubsequentActionPostedOnDispatcherThread()
        {
            AutoResetEvent gate = new AutoResetEvent(false);
            bool? pass = null;
            bool? wasRun = null;
            _dispatcher.Dispatch(() =>
            {
                _dispatcher.Dispatch(() =>
                {
                    // we can rely on this thread being the same as the outer 
                    // dispatcher call so we can make assumptions about it being 
                    // the only thread that can set wasRun.
                    pass = wasRun == true;
                    gate.Set();
                });
                wasRun = true;
            });
            gate.WaitOne();
            wasRun.Value.ShouldBe(true);
            pass.Value.ShouldBe(true);
        }

        [Test]
        public void DispatchQueuesSubsequentActionPostedOnOtherThread()
        {
            AutoResetEvent gate = new AutoResetEvent(false);
            List<int> processed = new List<int>();
            Action action = () =>
            {
                processed.Add(1);
                gate.Set();
                gate.WaitOne();
            };
            _dispatcher.Dispatch(action);
            _dispatcher.Dispatch(() => processed.Add(2));
            gate.WaitOne();
            processed.ShouldBe(new int[] { 1 });
            _dispatcher.Dispatch(() => processed.Add(3));
            _dispatcher.Dispatch(() =>
            {
                processed.Add(4);
                gate.Set();
            });
            gate.Set();
            gate.WaitOne();
            processed.ShouldBe(new int[] { 1, 2, 3, 4 });
        }

        [Test]
        public void DisposingFromDispatcherThreadStopFurtherProcessing()
        {
            AutoResetEvent gate = new AutoResetEvent(false);
            List<int> processed = new List<int>();
            _dispatcher.Dispatch(() =>
            {
                processed.Add(1);
                _dispatcher.Dispatch(() =>
                {
                    processed.Add(2);
                    gate.Set();
                });
                _dispatcher.Dispose();
            });
            gate.WaitOne(500).ShouldBe(false);
            processed.ShouldBe(new int[] { 1 });
            Assert.Throws<ObjectDisposedException>(() => _dispatcher.Dispatch(() => processed.Add(3)));
        }
    }
}