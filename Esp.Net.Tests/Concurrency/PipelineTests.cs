using System;
using System.Collections.Generic;
using Esp.Net.Reactive;
using Moq;
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
            public int AnInt { get; set; }
            public string AString { get; set; }
            public decimal ADecimal { get; set; }
        }

        public class InitialEvent { }
        public class AnAsyncEvent { }

        private Router<TestModel> _router;
        private TestModel _model;
        private TestSubject<string> _stringSubject;
        private TestSubject<int> _intSubject;
        private TestSubject<decimal> _decimalSubject;
        private Exception _exception;

        [SetUp]
        public void SetUp()
        {
            _model = new TestModel();
            _router = new Router<TestModel>(_model, RouterScheduler.Default);
            _stringSubject = new TestSubject<string>();
            _intSubject = new TestSubject<int>();
            _decimalSubject = new TestSubject<decimal>();
        }

        [Test]
        public void WhenStepResultAreContinueObservableIsSubscribed()
        {
            IPipeline<TestModel> pipeline = _router.ConfigurePipeline()
                .AddStep(ADelegateThatStatesToRunStringStep, OnStringStepResultsReceived)
                .Create();
            _stringSubject.Observers.Count.ShouldBe(0);
            pipeline.CreateInstance().Run(_model, OnError);
            _stringSubject.Observers.Count.ShouldBe(1);
        }

        [Test]
        public void WhenAsyncResultsReturndResultsDelegateInvoked()
        {
            _router
                .ConfigurePipeline()
                .AddStep(ADelegateThatStatesToRunStringStep, OnStringStepResultsReceived)
                .Create()
                .CreateInstance()
                .Run(_model, OnError);            
            _stringSubject.OnNext("Foo");
            _model.AString.ShouldBe("Foo");
        }

        [Test]
        public void StepStaysSubscribedToObservableUntilItCompletes()
        {
            // we need to introduce a stub/mock router so we can properly test things are getting disposed.
            // ATM thers is no good way to assert that the internal observations against the router are getting disposed.

            var router = new MockRouter<TestModel>(_model);
            router.SetUpEventStream<AyncResultsEvent<string>>();

            IPipeline<TestModel> pipeline = router.Object.ConfigurePipeline()
                .AddStep(ADelegateThatStatesToRunStringStep, OnStringStepResultsReceived)
                .Create();
            pipeline.CreateInstance().Run(_model, OnError);
            _stringSubject.Observers.Count.ShouldBe(1);
            _stringSubject.OnNext("Foo");

            _stringSubject.Observers.Count.ShouldBe(1);
            _stringSubject.OnCompleted();
            Assert.Inconclusive();
            _stringSubject.Observers.Count.ShouldBe(0);
        }

        [Test]
        public void WhenStepCompletesItCallsNextStep()
        {
            _router
                .ConfigurePipeline()
                .AddStep(ADelegateThatStatesToRunStringStep, OnStringStepResultsReceived)
                .AddStep(ADelegateThatStatesToRunRunDecimalStep, OnDecialStepResultsReceived)
                .Create()
                .CreateInstance()
                .Run(_model, OnError);
            _stringSubject.OnNext("Foo");
            _stringSubject.Observers.Count.ShouldBe(1);
            _decimalSubject.Observers.Count.ShouldBe(1);
        }

        private StepResult<string> ADelegateThatStatesToRunStringStep(TestModel model)
        {
            return StepResult<string>.SubscribeTo(_stringSubject);
        }

        private void OnStringStepResultsReceived(TestModel model, string results)
        {
            model.AString = results;
        }

        private StepResult<decimal> ADelegateThatStatesToRunRunDecimalStep(TestModel model)
        {
            return StepResult<decimal>.SubscribeTo(_decimalSubject);
        }

        private void OnDecialStepResultsReceived(TestModel model, decimal results)
        {
            model.ADecimal= results;
        }

        private StepResult<int> ADelegateThatStatesToRunIntStep(TestModel model)
        {
            return StepResult<int>.SubscribeTo(_intSubject);
        }

        private void OnIntStepResultsReceived(TestModel model, int results)
        {
            model.AnInt = results;
        }

        private void OnError(Exception ex)
        {
            _exception = ex;
        }
    }

    public class MockRouter<TModel> : Mock<IRouter<TModel>>
    {
        private readonly TModel _model;
        private readonly Dictionary<Type, object> _eventSubjects = new Dictionary<Type, object>();

        public MockRouter(TModel model)
        {
            _model = model;
        }

        public void SetUpEventStream<TEvent>()
        {
            var subject = new EventSubject<TModel, TEvent, IEventContext>();
            Setup(r => r.GetEventObservable<TEvent>(ObservationStage.Normal))
                .Returns(subject);
            _eventSubjects.Add(typeof(TEvent), subject);
            Setup(r => r.PublishEvent(It.IsAny<TEvent>())).Callback((TEvent e) =>
            {
                PublishEvent(e);
            });
        }

        public void PublishEvent<TEvent>(TEvent e)
        {
            dynamic subject = _eventSubjects[typeof (TEvent)];
            subject.OnNext(_model, e, new EventContext());
        }
    }
}
#endif