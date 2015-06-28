using NUnit.Framework;
using Shouldly;

#if ESP_EXPERIMENTAL
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

        private IRouter<TestModel> _router;
        private TestModel _model;
        private TestSubject<string> _asyncSubject;

        [SetUp]
        public void SetUp()
        {
            _model = new TestModel();
            _router = new Router<TestModel>(_model, RouterScheduler.Default);
            _asyncSubject = new TestSubject<string>();
        }

        [Test]
        public void OnInitialEventInvokesAsyncWorkFactory()
        {
            bool factoryInvoked = false;
            _router
                 .GetEventObservable<int>()
                 .BeginAcync(
                     (model, @event, context) =>
                     {
                         factoryInvoked = true;
                         return _asyncSubject;
                     }, _router)
                 .Observe((m, e, c) =>
                 {

                 });
            _router.PublishEvent(1);
            factoryInvoked.ShouldBe(true);
        }

        [Test]
        public void DoesNotCallFactoryWhenObservableDisposed()
        {
            bool factoryInvoked = false;
            var disposable = _router
                 .GetEventObservable<int>()
                 .BeginAcync(
                     (model, @event, context) =>
                     {
                         factoryInvoked = true;
                         return _asyncSubject;
                     }, _router)
                 .Observe((m, e, c) =>
                 {

                 });
            disposable.Dispose();
            _router.PublishEvent(1);
            factoryInvoked.ShouldBe(false);
        }

        [Test]
        public void OnAcyncResultsReceivedCallObserver()
        {
            TestModel receivedModel = null;
            string receivedResults = null;
            IEventContext receivedEventContext = null;
            int receivedCount = 0;
            _router
                 .GetEventObservable<int>()
                 .BeginAcync((model, @event, context) => _asyncSubject, _router)
                 .Observe((m, e, c) =>
                 {
                     receivedCount++;
                     receivedModel = m;
                     receivedResults = e.Result;
                     receivedEventContext = c;
                 });
            _router.PublishEvent(1);
            _asyncSubject.OnNext("asyncResults");
            receivedCount.ShouldBe(1);
            receivedModel.ShouldBeSameAs(_model);
            receivedResults.ShouldBe("asyncResults");
            receivedEventContext.ShouldNotBe(null);
        }
    }
}
#endif