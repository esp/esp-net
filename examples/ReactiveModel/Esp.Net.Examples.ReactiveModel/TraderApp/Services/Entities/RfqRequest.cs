using System;

namespace Esp.Net.Examples.ReactiveModel.TraderApp.Services.Entities
{
    public class RfqRequest
    {
        public RfqRequest(Guid quoteId, CurrencyPair currencyPair, decimal notional)
        {
            QuoteId = quoteId;
            CurrencyPair = currencyPair;
            Notional = notional;
        }

        public Guid QuoteId { get; private set; }
        public CurrencyPair CurrencyPair { get; private set; }
        public decimal Notional { get; private set; }
    }
}