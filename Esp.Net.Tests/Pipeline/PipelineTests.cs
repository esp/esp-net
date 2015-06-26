using System;
using System.Collections.Generic;
using Esp.Net.Model;
using NUnit.Framework;
using Shouldly;

namespace Esp.Net.Pipeline
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

        private Router<TestModel> _router;
        private TestModel _model;

        [SetUp]
        public void SetUp()
        {
            _model = new TestModel();
            _router = new Router<TestModel>(_model, RouterScheduler.Default);
        }

        // This is just a proof of concept test... much more to come
        [Test]
        public void PipeLineRunsSteps()
        {
            var step1Subject = new TestSubject<string>();
            var step2Subject = new TestSubject<int>();

            IPipeline<TestModel> pipeline = _router.ConfigurePipeline()
                .AddStep(
                    model => StepResult<string>.Continue(step1Subject),
                    (model, results) => model.AString = results                    
                )
                .AddStep(
                    model =>
                    {
                        model.ADecimal = 1.1m;
                        return StepResult.Continue();
                    }
                )
                .AddStep(
                    model => StepResult<int>.Continue(step2Subject),
                    (model, result) => model.AnInt = result
                )
                .Create();

            IPipelinInstance<TestModel> instance = pipeline.CreateInstance();
            instance.Run(_model);

            step1Subject.OnNext("Foo");
            step2Subject.OnNext(1);

            _model.AString.ShouldBe("Foo");
            _model.ADecimal.ShouldBe(1.1m);
            _model.AnInt.ShouldBe(1);
        }

        [Test]
        public void Foo()
        {
            var step1Subject = new TestSubject<string>();

            _router
                .GetEventObservable<int>()
                .RunAsyncOperation(context => step1Subject)
                .Observe(
                    (IEventContext<TestModel, AsyncResultsEvent<string>> context) =>
                    {
                        
                    });

//            _router.ConfigurePipeline()
//                .AddStep(
//                    model => StepResult<string>.Continue(step1Subject),
//                    (model, results) => { }
//                )
//                .StartOn<int>();
        }

        public class InitialEvent { }
        public class AnAsyncEvent { }

        public class TestSubject<T> : IObservable<T>, IObserver<T>
        {
            private readonly List<IObserver<T>> _observers = new List<IObserver<T>>();

            public void OnNext(T item)
            {
                foreach (IObserver<T> observer in _observers.ToArray())
                {
                    observer.OnNext(item);
                }
            }

            public void OnError(Exception error)
            {
                foreach (IObserver<T> observer in _observers.ToArray())
                {
                    observer.OnError(error);
                }
            }

            public void OnCompleted()
            {
                foreach (IObserver<T> observer in _observers.ToArray())
                {
                    observer.OnCompleted();
                }
            }

            public IDisposable Subscribe(IObserver<T> observer)
            {
                _observers.Add(observer);
                return EspDisposable.Create(() => _observers.Remove(observer));
            }
        }
    }
}