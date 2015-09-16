using System;

namespace Esp.Net.Examples.ReactiveModel.ClientApp.Services.Entities
{
    public class RfqResponse
    {
        public RfqResponse(Guid quoteId, CurrencyPair currencyPair, decimal notional, decimal? rate, QuoteStatus quoteStatus, bool isLastMessage = false)
        {
            QuoteId = quoteId;
            CurrencyPair = currencyPair;
            Notional = notional;
            Rate = rate;
            QuoteStatus = quoteStatus;
            IsLastMessage = isLastMessage;
        }

        public Guid QuoteId { get; private set; }
        public CurrencyPair CurrencyPair { get; private set; }
        public decimal Notional { get; private set; }
        public decimal? Rate { get; private set; }
        public QuoteStatus QuoteStatus { get; private set; }
        public bool IsLastMessage { get; private set; }
    }
}