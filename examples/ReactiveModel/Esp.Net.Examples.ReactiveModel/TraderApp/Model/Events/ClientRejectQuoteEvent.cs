using System;

namespace Esp.Net.Examples.ReactiveModel.TraderApp.Model.Events
{
    public class ClientRejectQuoteEvent
    {
        public ClientRejectQuoteEvent(Guid quoteId)
        {
            QuoteId = quoteId;
        }

        public Guid QuoteId { get; private set; }
    }
}