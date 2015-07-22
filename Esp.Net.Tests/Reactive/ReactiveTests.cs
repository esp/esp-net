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
using Esp.Net.Model;
using NUnit.Framework;
using Shouldly;

namespace Esp.Net.Reactive
{
    [TestFixture]
    public class ReactiveTests
    {
        public class TestModel
        {
        }

        private TestModel _model;

        private IEventContext _eventContext;

        private StubEventObservationRegistrar _eventObservationRegistrar;
        
        [SetUp]
        public void SetUp()
        {
            _model = new TestModel();
            _eventContext = new EventContext();
            _eventObservationRegistrar = new StubEventObservationRegistrar();
        }

        [Test]
        public void SubjectOnNextsItems()
        {
            var subject = new EventSubject<TestModel, int, IEventContext>(_eventObservationRegistrar);
            TestModel receivedModel = null;
            int receivedEvent = 0;
            IEventContext receivedContext = null;
            subject.Observe((m, e, c) =>
            {
                receivedModel = m;
                receivedEvent = e;
                receivedContext = c;
            });
            subject.OnNext(_model, 1, _eventContext);
            receivedModel.ShouldBeSameAs(_model);
            receivedEvent.ShouldBe(1);
            receivedContext.ShouldBeSameAs(_eventContext);
        }

        [Test]
        public void SubjectRemovesSubscriptionOnDispose()
        {
            var subject = new EventSubject<TestModel, int, IEventContext>(_eventObservationRegistrar);
            int received = 0;
            var disposable = subject.Observe((m, e, c) => received = e);
            subject.OnNext(_model, 1, _eventContext);
            Assert.AreEqual(1, received);
            disposable.Dispose();
            subject.OnNext(_model, 2, _eventContext);
            Assert.AreEqual(1, received);
        }

        [Test]
        public void WhereFiltersWithProvidedPredicate()
        {
            var subject = new EventSubject<TestModel, int, IEventContext>(_eventObservationRegistrar);
            List<int> received = new List<int>();
            subject
                .Where((m, e, c) => e%2 == 0)
                .Observe((m, e, c) => received.Add(e));
            for (int i = 0; i < 10; i++) subject.OnNext(_model, i, _eventContext);
            Assert.IsTrue(received.SequenceEqual(new[] {0, 2, 4, 6, 8}));
        }

        [Test]
        public void WhereChainsSourceDisposableOnDispose()
        {
            var mockIEventObservable = new StubEventObservable<TestModel>();
            var disposable = mockIEventObservable
                .Where((m, e, c) => true)
                .Observe((m, e, c) => { });
            disposable.Dispose();
            Assert.IsTrue(mockIEventObservable.IsDisposed);
        }

        [Test]
        public void CanConcatEventStreams()
        {
            var subject1 = new EventSubject<TestModel, int, IEventContext>(_eventObservationRegistrar);
            var subject2 = new EventSubject<TestModel, int, IEventContext>(_eventObservationRegistrar);
            var stream = EventObservable.Concat(subject1, subject2);
            List<int> received = new List<int>();
            stream.Observe((m, e, c) => received.Add(e));
            subject1.OnNext(_model, 1, _eventContext);
            subject2.OnNext(_model, 2, _eventContext);
            subject1.OnNext(_model, 3, _eventContext);
            subject2.OnNext(_model, 4, _eventContext);
            Assert.IsTrue(received.SequenceEqual(new[] {1, 2, 3, 4}));
        }

        [Test]
        public void TakeOnlyTakesGivenNumberOfEvents()
        {
            List<int> received = new List<int>();
            var subject1 = new EventSubject<TestModel, int, IEventContext>(_eventObservationRegistrar);
            subject1.Take(3).Observe((m, e, c) => received.Add(e));
            subject1.OnNext(_model, 1, _eventContext);
            subject1.OnNext(_model, 2, _eventContext);
            subject1.OnNext(_model, 3, _eventContext);
            subject1.OnNext(_model, 4, _eventContext);
            Assert.IsTrue(received.SequenceEqual(new[] {1, 2, 3}));
        }

        [Test]
        public void TakeChainsSourceDisposableOnDispose()
        {
            var mockIEventObservable = new StubEventObservable<TestModel>();
            var disposable = mockIEventObservable.Take(3).Observe((m, e, c) => { });
            disposable.Dispose();
            Assert.IsTrue(mockIEventObservable.IsDisposed);
        }

        [Test]
        public void IncrementsObservationRegistrarOnObserve()
        {
        }

        private class StubEventObservationRegistrar : IEventObservationRegistrar
        {
            public StubEventObservationRegistrar()
            {
                Register = new Dictionary<Type, int>();
            }

            public Dictionary<Type, int> Register { get; private set; }
          
            public void IncrementRegistration<TEvent>()
            {
                if (Register.ContainsKey(typeof (TEvent)))
                {
                    Register[typeof (TEvent)]++;
                }
                else
                {
                    Register[typeof(TEvent)] = 1;
                }
            }

            public void DecrementRegistration<TEvent>()
            {
                Register[typeof(TEvent)]--;
            }
        }
    }
}