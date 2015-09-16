using System;

namespace Esp.Net.Examples.ReactiveModel.Common.Dtos
{
    public class RfqResponseDto
    {
        public RfqResponseDto(Guid quoteId, CurrencyPairDto currencyPair, decimal notional, decimal rate, QuoteStatusDto quoteStatusDto, bool isLastMessage = false)
        {
            QuoteId = quoteId;
            CurrencyPair = currencyPair;
            Notional = notional;
            Rate = rate;
            QuoteStatusDto = quoteStatusDto;
            IsLastMessage = isLastMessage;
        }

        public Guid QuoteId { get; private set; }
        public CurrencyPairDto CurrencyPair { get; private set; }
        public decimal Notional { get; private set; }
        public decimal Rate { get; private set; }
        public QuoteStatusDto QuoteStatusDto { get; private set; }
        public bool IsLastMessage { get; private set; }
    }
}