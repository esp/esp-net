using NUnit.Framework;
using Shouldly;

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

        [SetUp]
        public void SetUp()
        {
            _model = new TestModel();
            _router = new Router<TestModel>(_model, RouterScheduler.Default);
        }

        // This is just a proof of concept test... much more to come
        [Test]
        public void ComplexAsyncExample()
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
    }
}