using System;

namespace Esp.Net.Examples.ReactiveModel.TraderApp.Model.Events
{
    public class TraderSendQuoteEvent
    {
        public TraderSendQuoteEvent(Guid quoteId)
        {
            QuoteId = quoteId;
        }

        public Guid QuoteId { get; private set; }
    }
}