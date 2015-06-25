using System;
using Esp.Net.Pipeline;
using Esp.Net.Reactive;
using NUnit.Framework;

namespace Esp.Net
{
    [TestFixture]
    public class PipelineTests
    {
        private Router<TestModel> _router;
        private TestModel _model;

        [SetUp]
        public void SetUp()
        {
            _model = new TestModel();
            _router = new Router<TestModel>(_model, RouterScheduler.Default);
        }

        [Test]
        public void Foo()
        {
            var step1Subject = new StubSubject<string>();
            var step2Subject = new StubSubject<int>();

            var pipeline = _router.ConfigurePipeline()
                .On<InitialEvent>(
                    context =>
                    {
                        return PipelineStepResult.Run();
                    })
                .Then(
                    model =>
                    {
                        return new PipelineStepResult<string>(step1Subject);
                    },
                    (model, resultEvent) =>
                    {

                    })
                .Then(
                    model =>
                    {
                        return new PipelineStepResult<int>(step2Subject);
                    },
                    (model, resultEvent) =>
                    {

                    })
                .Create();

//            IPipelineInstance pipelineInstance = pipeline.CreateInstance();
//            var instanceState = new object(); // TODO state forms part of the pipeline
//            pipelineInstance.Start(_model, instanceState);
//            pipelineInstance.Dispose();
        }

        public class InitialEvent { }
        public class AnAsyncEvent { }

        private class StubSubject<T> : IObservable<T>
        {
            public IDisposable Subscribe(IObserver<T> observer)
            {
                throw new NotImplementedException();
            }
        }
    }
}