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

#if ESP_EXPERIMENTAL

using System;
using System.Collections.Generic;
using System.Linq;
using Esp.Net.Reactive;
using NUnit.Framework;
using Shouldly;

namespace Esp.Net.Workflow
{
    [TestFixture]
    public class WorkflowTests
    {
        public class TestModel
        {
            public TestModel()
            {
                ReceivedInts = new List<int>();
                ReceivedStrings = new List<string>();
                ReceivedDecimals = new List<decimal>();
                Id = Guid.NewGuid();
            }
            public Guid Id { get; private set; }
            public List<int> ReceivedInts { get; set; }
            public List<string> ReceivedStrings { get; set; }
            public List<decimal> ReceivedDecimals { get; set; }
        }

        public class InitialEvent { }
        public class AnAsyncEvent { }

        private Router _router;
        private TestModel _model;
        private StubSubject<string> _stringSubject;
        private StubSubject<int> _intSubject;
        private StubSubject<decimal> _decimalSubject;
        private Exception _exception;

        [SetUp]
        public void SetUp()
        {
            _model = new TestModel();
            _router = new Router(new StubRouterDispatcher());
            _router.AddModel(_model.Id, _model);
            _stringSubject = new StubSubject<string>();
            _intSubject = new StubSubject<int>();
            _decimalSubject = new StubSubject<decimal>();
        }

        [Test]
        public void WhenAsyncResultsReturndResultsDelegateInvoked()
        {
            _router
                .ConfigureWorkflow<TestModel, InitialEvent>()
                .SelectMany(GetStringObservble, OnStringResultsReceived)
                .Run(_model.Id, OnError, OnCompleted);    
            _router.PublishEvent(_model.Id, new InitialEvent());
            _stringSubject.OnNext("Foo");
            _stringSubject.OnNext("Bar");
            _stringSubject.OnNext("Baz");
            _model.ReceivedStrings.SequenceEqual(new[] {"Foo", "Bar", "Baz"}).ShouldBe(true);
        }

        [Test]
        public void StepStaysSubscribedToObservableUntilItCompletes()
        {
            // we need to introduce a stub/mock router so we can properly test things are getting disposed.
            // ATM thers is no good way to assert that the internal observations against the router are getting disposed.

            _router
                .ConfigureWorkflow<TestModel, InitialEvent>()
                .SelectMany(GetStringObservble, OnStringResultsReceived)
                .Run(_model.Id, OnError, OnCompleted);

            _router.PublishEvent(_model.Id, new InitialEvent());

            GetEventObserverCount(typeof(AyncResultsEvent<string>)).ShouldBe(1);
                
            _stringSubject.Observers.Count.ShouldBe(1);
            
            _stringSubject.OnCompleted();

            GetEventObserverCount(typeof(AyncResultsEvent<string>)).ShouldBe(0);
        }

        [Test]
        public void WhenStepProcessessResultsItThenCallsNextStep()
        {
//            var stringEventObservable = _router.GetEventSubject<AyncResultsEvent<string>>();
//            var decimalEventObservable = _router.GetEventSubject<AyncResultsEvent<decimal>>(); 
//            
            _router
                .ConfigureWorkflow<TestModel, InitialEvent>()
                .SelectMany(GetStringObservble, OnStringResultsReceived)
                .SelectMany(GetDecimalObservable, OnDecialResultsReceived)
                .Run(_model.Id, OnError, OnCompleted);

            _router.PublishEvent(_model.Id, new InitialEvent());

            _stringSubject.OnNext("Foo");
            _stringSubject.Observers.Count.ShouldBe(1);
            GetEventObserverCount(typeof(AyncResultsEvent<string>)).ShouldBe(1);
            _decimalSubject.Observers.Count.ShouldBe(1);
            GetEventObserverCount(typeof(AyncResultsEvent<decimal>)).ShouldBe(1);

            _stringSubject.OnNext("Bar");
            _stringSubject.Observers.Count.ShouldBe(1);
            GetEventObserverCount(typeof(AyncResultsEvent<string>)).ShouldBe(1);
            _decimalSubject.Observers.Count.ShouldBe(2);
            GetEventObserverCount(typeof(AyncResultsEvent<decimal>)).ShouldBe(2);

            _decimalSubject.OnNext(1);
            _model.ReceivedDecimals.SequenceEqual(new [] {1m, 1m}).ShouldBe(true);

            _stringSubject.OnCompleted();
            GetEventObserverCount(typeof(AyncResultsEvent<string>)).ShouldBe(0);

            // note that even though the prior step completed the next stays subscribed, this is in line with IObservabe<T>.SelectMany (from Rx).
            GetEventObserverCount(typeof(AyncResultsEvent<decimal>)).ShouldBe(2);
        }

        private IObservable<string> GetStringObservble(TestModel model, IWorkflowInstanceContext context)
        {
            return _stringSubject;
        }

        private void OnStringResultsReceived(TestModel model, IWorkflowInstanceContext context, string results)
        {
            model.ReceivedStrings.Add(results);
        }

        private IObservable<decimal> GetDecimalObservable(TestModel model, IWorkflowInstanceContext context)
        {
            return _decimalSubject;
        }

        private void OnDecialResultsReceived(TestModel model, IWorkflowInstanceContext context, decimal results)
        {
            model.ReceivedDecimals.Add(results);
        }

        private IObservable<int> GetIntObservable(TestModel model, IWorkflowInstanceContext context)
        {
            return _intSubject;
        }

        private void OnIntResultsReceived(TestModel model, IWorkflowInstanceContext context, int results)
        {
            model.ReceivedInts.Add(results);
        }

        private void OnError(TestModel model, IWorkflowInstanceContext context, Exception ex)
        {
            _exception = ex;
        }

        private void OnCompleted(TestModel model, IWorkflowInstanceContext context)
        {
        }

        private int GetEventObserverCount(Type eventType)
        {
            return _router
                .EventsObservationRegistrar
                .GetEventObservationCount(_model.Id, eventType);
        }
    }
}
#endif