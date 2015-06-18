using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Esp.Net.Reactive
{
    [TestFixture]
    public class ReactiveTests
    {
        [Test]
        public void SubjectOnNextsItems()
        {
            var subject = new EventSubject<int>();
            int received = 0;
            subject.Observe(i => received = i);
            subject.OnNext(1);
            Assert.AreEqual(1, received);
            subject.OnNext(2);
            Assert.AreEqual(2, received);
        }

        [Test]
        public void SubjectRemovesSubscriptionOnDispose()
        {
            var subject = new EventSubject<int>();
            int received = 0;
            var disposable = subject.Observe(i => received = i);
            subject.OnNext(1);
            Assert.AreEqual(1, received);
            disposable.Dispose();
            subject.OnNext(2);
            Assert.AreEqual(1, received);
        }

        [Test]
        public void WhereFiltersWithProvidedPredicate()
        {
            var subject = new EventSubject<int>();
            List<int> received = new List<int>();
            subject
                .Where(i => i % 2 == 0)
                .Observe(i => received.Add(i));
            for (int i = 0; i < 10; i++) subject.OnNext(i);
            Assert.IsTrue(received.SequenceEqual(new[]{0, 2, 4, 6, 8}));
        }

        [Test]
        public void WhereChainsSourceDisposableOnDispose()
        {
            var mockIEventObservable = new MockIEventObservable();
            var disposable = mockIEventObservable
                .Where(i => true)
                .Observe(i => { });
            disposable.Dispose();
            Assert.IsTrue(mockIEventObservable.IsDisposed);
        }

        [Test]
        public void CanConcatEventStreams()
        {
            var subject1 = new EventSubject<int>();
            var subject2 = new EventSubject<int>();
            var stream = EventObservable.Concat(subject1, subject2);
            List<int> received = new List<int>();
            stream.Observe(i => received.Add(i));
            subject1.OnNext(1);
            subject2.OnNext(2);
            subject1.OnNext(3);
            subject2.OnNext(4);
            Assert.IsTrue(received.SequenceEqual(new[] { 1, 2, 3, 4 }));
        }

        private class MockIEventObservable : IEventObservable<int>
        {
            public bool IsDisposed { get; private set; }

            public IDisposable Observe(Action<int> onNext)
            {
                return Disposable.Create(() => IsDisposed = true);
            }

            public IDisposable Observe(IEventObserver<int> observer)
            {
                return Disposable.Create(() => IsDisposed = true);
            }
        }
    }
}