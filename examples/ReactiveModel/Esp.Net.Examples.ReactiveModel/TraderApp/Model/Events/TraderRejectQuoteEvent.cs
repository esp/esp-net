using System;

namespace Esp.Net.Examples.ReactiveModel.TraderApp.Model.Events
{
    public class TraderRejectQuoteEvent
    {
        public TraderRejectQuoteEvent(Guid quoteId)
        {
            QuoteId = quoteId;
        }

        public Guid QuoteId { get; private set; }
    }
}