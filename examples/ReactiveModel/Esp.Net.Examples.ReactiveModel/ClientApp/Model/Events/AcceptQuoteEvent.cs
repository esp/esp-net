using System;

namespace Esp.Net.Examples.ReactiveModel.ClientApp.Model.Events
{
    public class AcceptQuoteEvent
    {
        public AcceptQuoteEvent(Guid quoteId)
        {
            QuoteId = quoteId;
        }

        public Guid QuoteId { get; private set; }
    }
}