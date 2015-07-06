using System;
using System.Collections.Generic;
using System.Linq;
using Esp.Net.Stubs;
using NUnit.Framework;
using Shouldly;

#if ESP_EXPERIMENTAL
namespace Esp.Net.Concurrency
{
    [TestFixture]
    public class PipelineTests
    {
        public class TestModel
        {
            public TestModel()
            {
                ReceivedInts = new List<int>();
                ReceivedStrings = new List<string>();
                ReceivedDecimals = new List<decimal>();
            }
            public List<int> ReceivedInts { get; set; }
            public List<string> ReceivedStrings { get; set; }
            public List<decimal> ReceivedDecimals { get; set; }
        }

        public class InitialEvent { }
        public class AnAsyncEvent { }

        private StubRouter<TestModel> _router;
        private TestModel _model;
        private StubSubject<string> _stringSubject;
        private StubSubject<int> _intSubject;
        private StubSubject<decimal> _decimalSubject;
        private Exception _exception;

        [SetUp]
        public void SetUp()
        {
            _model = new TestModel();
            _router = new StubRouter<TestModel>(_model);
            _stringSubject = new StubSubject<string>();
            _intSubject = new StubSubject<int>();
            _decimalSubject = new StubSubject<decimal>();
        }

        [Test]
        public void WhenAsyncResultsReturndResultsDelegateInvoked()
        {
            _router
                .ConfigurePipeline<TestModel, InitialEvent>()
                .SelectMany(GetStringObservble, OnStringResultsReceived)
                .Run(OnError);    
            _router.PublishEvent(new InitialEvent());
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

            var eventSubject = _router.GetEventSubject<AyncResultsEvent<string>>();

            _router
                .ConfigurePipeline<TestModel, InitialEvent>()
                .SelectMany(GetStringObservble, OnStringResultsReceived)
                .Run(OnError);

            _router.PublishEvent(new InitialEvent());

            eventSubject.Observers.Count.ShouldBe(1);
            _stringSubject.Observers.Count.ShouldBe(1);
            
            _stringSubject.OnCompleted();

            eventSubject.Observers.Count.ShouldBe(0);
        }

        [Test]
        public void WhenStepProcessessResultsItThenCallsNextStep()
        {
            var stringEventObservable = _router.GetEventSubject<AyncResultsEvent<string>>();
            var decimalEventObservable = _router.GetEventSubject<AyncResultsEvent<decimal>>(); 
            
            _router
                .ConfigurePipeline<TestModel, InitialEvent>()
                .SelectMany(GetStringObservble, OnStringResultsReceived)
                .SelectMany(GetDecimalObservable, OnDecialResultsReceived)
                .Run(OnError);

            _router.PublishEvent(new InitialEvent());

            _stringSubject.OnNext("Foo");
            _stringSubject.Observers.Count.ShouldBe(1);
            stringEventObservable.Observers.Count.ShouldBe(1);
            _decimalSubject.Observers.Count.ShouldBe(1);
            decimalEventObservable.Observers.Count.ShouldBe(1);

            _stringSubject.OnNext("Bar");
            _stringSubject.Observers.Count.ShouldBe(1);
            stringEventObservable.Observers.Count.ShouldBe(1);
            _decimalSubject.Observers.Count.ShouldBe(2);
            decimalEventObservable.Observers.Count.ShouldBe(2);

            _decimalSubject.OnNext(1);
            _model.ReceivedDecimals.SequenceEqual(new [] {1m, 1m}).ShouldBe(true);

            _stringSubject.OnCompleted();
            stringEventObservable.Observers.Count.ShouldBe(0);

            // note that even though the prior step completed the next stays subscribed, this is in line with IObservabe<T>.SelectMany (from Rx).
            decimalEventObservable.Observers.Count.ShouldBe(2);
        }

        private IObservable<string> GetStringObservble(TestModel model, IPipelineInstanceContext context)
        {
            return _stringSubject;
        }

        private void OnStringResultsReceived(TestModel model, string results)
        {
            model.ReceivedStrings.Add(results);
        }

        private IObservable<decimal> GetDecimalObservable(TestModel model, IPipelineInstanceContext context)
        {
            return _decimalSubject;
        }

        private void OnDecialResultsReceived(TestModel model, decimal results)
        {
            model.ReceivedDecimals.Add(results);
        }

        private IObservable<int> GetIntObservable(TestModel model, IPipelineInstanceContext context)
        {
            return _intSubject;
        }

        private void OnIntResultsReceived(TestModel model, int results)
        {
            model.ReceivedInts.Add(results);
        }

        private void OnError(IPipelineInstanceContext context, Exception ex)
        {
            _exception = ex;
        }
    }
}
#endif