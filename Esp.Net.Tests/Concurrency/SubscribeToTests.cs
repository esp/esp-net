using System;
using NUnit.Framework;
using Shouldly;

#if ESP_EXPERIMENTAL
namespace Esp.Net.Concurrency
{
    [TestFixture]
    public class SubscribeToTests
    {
        public class TestModel
        {
            public int AnInt { get; set; }
            public string AString { get; set; }
            public decimal ADecimal { get; set; }
        }
        public class AnEvent { }
        public class AnAsyncResultsEvent : IdentifiableEvent {
            public AnAsyncResultsEvent(Guid id) : base(id)
            {
            }
        }
        private IRouter<TestModel> _router;
        private TestModel _model;
        private TestSubject<AnAsyncResultsEvent> _asyncResultsSubject;

        [SetUp]
        public void SetUp()
        {
            _model = new TestModel();
            _router = new Router<TestModel>(_model, RouterScheduler.Default);
            _asyncResultsSubject = new TestSubject<AnAsyncResultsEvent>();
        }

        [Test]
        public void OnInitialEventInvokesAsyncWorkFactory()
        {
            bool factoryInvoked = false;
            _router
                 .GetEventObservable<int>()
                 .SubscribeTo(
                     (model, @event, context) =>
                     {
                         factoryInvoked = true;
                         return _asyncResultsSubject;
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
                 .SubscribeTo(
                     (model, @event, context) =>
                     {
                         factoryInvoked = true;
                         return _asyncResultsSubject;
                     }, _router)
                 .Observe((m, e, c) =>
                 {

                 });
            disposable.Dispose();
            _router.PublishEvent(1);
            factoryInvoked.ShouldBe(false);
        }

        [Test]
        public void OnAcyncResultsReceivedCallsObserver()
        {
            TestModel receivedModel = null;
            AnAsyncResultsEvent receivedResults = null;
            IEventContext receivedEventContext = null;
            int receivedCount = 0;

            // Q: what if _asyncSubject yields many times?
            // A: we simply stay subscribed and raising events until it errors or completes 

            // Q what if _asyncSubject errors -  
            // A: the router should enter the halted state  

            // Q: what if _asyncSubject completes
            // A: then we go back to waiting for the next 'int' event, the EventObservable returned from the router doesn't complete

            // Q: what if we get many 'int' events, how does that affect the above
            // A: we should act like SelectMany and just invoke the next step for each yield 

            // Note we need to have AnAsyncResultsEvent derive from IdentifiableEvent as we need to 
            // be able to id this event as it will be published to the router and filtered by 'SubscribeTo'. This allows 
            // for the IEventContext associated with AnAsyncResultsEvent to be correct and thus you can call c.Commit() in the observer and 
            // take part in the staged event workflow as expected.
            _router
                 .GetEventObservable<AnEvent>()
                 .SubscribeTo((model, @event, context) => _asyncResultsSubject, _router)
                 .Observe((TestModel m, AnAsyncResultsEvent e, IEventContext c) =>
                 {
                     receivedCount++;
                     receivedModel = m;
                     receivedResults = e;
                     receivedEventContext = c;
                 });
            _router.PublishEvent(new AnEvent());
            var anAsyncResultsEvent = new AnAsyncResultsEvent(Guid.NewGuid());
            _asyncResultsSubject.OnNext(anAsyncResultsEvent);
            receivedCount.ShouldBe(1);
            receivedModel.ShouldBeSameAs(_model);
            receivedResults.ShouldBeSameAs(anAsyncResultsEvent);
            receivedEventContext.ShouldNotBe(null);
        }
    }
}
#endif