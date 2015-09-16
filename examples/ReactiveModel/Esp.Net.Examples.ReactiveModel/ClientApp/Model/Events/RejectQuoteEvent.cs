using System;

namespace Esp.Net.Examples.ReactiveModel.ClientApp.Model.Events
{
    public class RejectQuoteEvent
    {
        public RejectQuoteEvent(Guid quoteId)
        {
            QuoteId = quoteId;
        }

        public Guid QuoteId { get; private set; }
    }
}