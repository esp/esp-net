using System;

namespace Esp.Net.Examples.ReactiveModel.Common.Dtos
{
    public class RfqRequestDto
    {
        public RfqRequestDto(Guid quoteId, CurrencyPairDto currencyPair, decimal notional)
        {
            QuoteId = quoteId;
            CurrencyPair = currencyPair;
            Notional = notional;
        }

        public Guid QuoteId { get; private set; }
        public CurrencyPairDto CurrencyPair { get; private set; }
        public decimal Notional { get; private set; }
    }
}