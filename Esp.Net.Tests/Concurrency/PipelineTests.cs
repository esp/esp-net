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

        [SetUp]
        public void SetUp()
        {
            _model = new TestModel();
            _router = new Router<TestModel>(_model, RouterScheduler.Default);
            _stringSubject = new TestSubject<string>();
            _intSubject = new TestSubject<int>();
            _decimalSubject = new TestSubject<decimal>();
        }

        // This is just a proof of concept test... much more to come
        [Test]
        public void CallEachStageInThePipeline()
        {
            IPipeline<TestModel> pipeline = _router.ConfigurePipeline()
                .AddStep(ShouldRunStringStep, OnStringStepResultsReceived)
                .AddStep(ShouldRunDecimalStep, OnDecialStepResultsReceived)
                .AddStep(ShouldRunIntStep, OnIntStepResultsReceived)
                .Create();

            IPipelinInstance<TestModel> instance = pipeline.CreateInstance();
            instance.Run(_model, ex => { });

            _stringSubject.OnNext("Foo");
            _decimalSubject.OnNext(1.1m);
            _intSubject.OnNext(1);

            _model.AString.ShouldBe("Foo");
            _model.ADecimal.ShouldBe(1.1m);
            _model.AnInt.ShouldBe(1);
        }

        private StepResult<string> ShouldRunStringStep(TestModel model)
        {
            return StepResult<string>.Continue(_stringSubject);
        }

        private void OnStringStepResultsReceived(TestModel model, string results)
        {
            model.AString = results;
        }

        private StepResult<decimal> ShouldRunDecimalStep(TestModel model)
        {
            return StepResult<decimal>.Continue(_decimalSubject);
        }

        private void OnDecialStepResultsReceived(TestModel model, decimal results)
        {
            model.ADecimal= results;
        }

        private StepResult<int> ShouldRunIntStep(TestModel model)
        {
            return StepResult<int>.Continue(_intSubject);
        }

        private void OnIntStepResultsReceived(TestModel model, int results)
        {
            model.AnInt = results;
        }
    }
}
#endif