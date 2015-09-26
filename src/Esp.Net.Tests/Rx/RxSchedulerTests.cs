using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Esp.Net.Rx
{
    public class RxSchedulerTests
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
    }
}
