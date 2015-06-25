#region copyright
// Copyright 2015 Keith Woods
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion
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

        [Test]
        public void TakeOnlyTakesGivenNumberOfEvents()
        {
            List<int> received = new List<int>();
            var subject1 = new EventSubject<int>();
            subject1.Take(3).Observe(i => received.Add(i));
            subject1.OnNext(1);
            subject1.OnNext(2);
            subject1.OnNext(3);
            subject1.OnNext(4);
            Assert.IsTrue(received.SequenceEqual(new[] { 1, 2, 3 }));
        }

        [Test]
        public void TakeChainsSourceDisposableOnDispose()
        {
            var mockIEventObservable = new MockIEventObservable();
            var disposable = mockIEventObservable.Take(3).Observe(i => { });
            disposable.Dispose();
            Assert.IsTrue(mockIEventObservable.IsDisposed);
        }

        private class MockIEventObservable : IEventObservable<int>
        {
            public bool IsDisposed { get; private set; }

            public IDisposable Observe(Action<int> onNext)
            {
                return EspDisposable.Create(() => IsDisposed = true);
            }

            public IDisposable Observe(IEventObserver<int> observer)
            {
                return EspDisposable.Create(() => IsDisposed = true);
            }
        }
    }
}