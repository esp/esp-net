#if ESP_EXPERIMENTAL
using System;
using Esp.Net.Model;
using Esp.Net.Workflow;


namespace Esp.Net.Examples
{
    public class FxOption
    {
        public string CurrencyPair { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string CurrentQuoteId { get; set; }
    }

    public class AcceptQuoteEvent
    {
        public string QuoteId { get; set; }
    }

    public class BookingWorkflowContext : IWorkflowInstanceContext
    {
        private bool _isCanceled;

        public BookingWorkflowContext(AcceptQuoteEvent initialEvent)
        {
            Event = initialEvent;
        }

        public AcceptQuoteEvent Event { get; private set; }

        public bool IsCanceled
        {
            get { return _isCanceled; }
        }

        public void Cancel()
        {
            _isCanceled = true;
        }
    }

    public class ReferenceDatesEventProcessor  : DisposableBase, IEventProcessor
    {
        private readonly IRouter<FxOption> _router;
        private readonly IBookingService _bookingService;
        private readonly EspSerialDisposable _inflightWorkItem = new EspSerialDisposable();

        public ReferenceDatesEventProcessor(IRouter<FxOption> router, IBookingService bookingService)
        {
            _router = router;
            _bookingService = bookingService;
            AddDisposable(_inflightWorkItem);
        }

        public void Start()
        {
            // By adding a workflow context we can flow that right through the stack and provide it 
            // anytime we invoke a deletage, for example on each step or on a workflow instance exception.
            //
            // We can solve the problem of if a step'should run' not by returning empty observables (which the consumer may not own)
            // but rather by using the context. Similar to the GetEventObservable api we can just cancel the contexxt 
            IDisposable disposable = _router
                .ConfigureWorkflow<FxOption, BookingWorkflowContext, AcceptQuoteEvent>((m, e, c) => new BookingWorkflowContext(e))
                // select many functions much the same as select many in Rx, we stay subscribed to the 
                // response stream and invoke the next step for each yield. If the stream completes 
                // the workflow instance will stay subscribed to the next stream until that completes.
                .SelectMany((model, workflowContext) => _bookingService.AcceptQuote(model.CurrentQuoteId), OnQuoteAccepted)
                .SelectMany((model, workflowContext) => _bookingService.GenerateTermsheet(model.CurrentQuoteId), OnTermsheetReceived)
                .Do(OnBookingComlete)
                // Run wraps Create and for each event creates a new workflow instance (via CreateInstance).
                // so efictively each instance acts in it's own right, however all instances can be 
                // disposed usng the disposable returned from Run().
                .Run(
                    (model, pipelinContext, exception) => { }, 
                    (model, context) => { }
                );
        }

        private void OnQuoteAccepted(FxOption model, BookingWorkflowContext context, string response)
        {
            // apply dates to model
        }

        private void OnTermsheetReceived(FxOption model, BookingWorkflowContext context, string response)
        {
            // apply dates to model
        }

        private void OnBookingComlete(FxOption model, BookingWorkflowContext context)
        {
        }

    }
}
#endif