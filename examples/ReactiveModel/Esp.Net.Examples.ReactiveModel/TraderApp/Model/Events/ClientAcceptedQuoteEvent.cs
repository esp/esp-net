using System;

namespace Esp.Net.Examples.ReactiveModel.TraderApp.Model.Events
{
    public class ClientAcceptedQuoteEvent
    {
        public ClientAcceptedQuoteEvent(Guid quoteId)
        {
            QuoteId = quoteId;
        }

        public Guid QuoteId { get; private set; }
    }
}