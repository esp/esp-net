using System;
using Esp.Net.Examples.ReactiveModel.ClientApp.Services.Entities;

namespace Esp.Net.Examples.ReactiveModel.ClientApp.Model.Events
{
    public class OrderResponseReceivedEvent
    {
        public OrderResponseReceivedEvent(Guid quoteId, CurrencyPair currencyPair, decimal notional, decimal? rate, bool isLastMessage, QuoteStatus status)
        {
            QuoteId = quoteId;
            CurrencyPair = currencyPair;
            Notional = notional;
            Rate = rate;
            IsLastMessage = isLastMessage;
            Status = status;
        }

        public OrderResponseReceivedEvent(Exception exception)
        {
            Exception = exception;
            HasException = true;
        }

        public bool HasException { get; private set; }
        public Exception Exception { get; private set; }
        public Guid QuoteId { get; private set; }
        public CurrencyPair CurrencyPair { get; private set; }
        public decimal Notional { get; private set; }
        public decimal? Rate { get; private set; }
        public QuoteStatus Status { get; private set; }
        public bool IsLastMessage { get; private set; }
    }
}