using System;

namespace Esp.Net.Examples.ReactiveModel.TraderApp.Model.Events
{
    public class ClientClosedQuoteEvent
    {
        public ClientClosedQuoteEvent(Guid quoteId)
        {
            QuoteId = quoteId;
        }

        public Guid QuoteId { get; private set; }
    }
}