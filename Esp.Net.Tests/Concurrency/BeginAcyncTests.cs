using Esp.Net.Model;
using NUnit.Framework;

namespace Esp.Net.Concurrency
{
    [TestFixture]
    public class BeginAcyncTests
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

        [Test]
        public void SimpleAsyncExample()
        {
            var step1Subject = new TestSubject<string>();

            _router
                .GetEventObservable<int>()
                .BeginAcync((model, @event, context) => step1Subject, _router)
                .Observe((TestModel m, AsyncResultsEvent<string> e, IEventContext c) =>
                {

                });
        }
    }
}