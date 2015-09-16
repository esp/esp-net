using System;
using Esp.Net.Examples.ReactiveModel.ClientApp.Services.Entities;

namespace Esp.Net.Examples.ReactiveModel.ClientApp.Model.Gateways
{
    public interface IRequestForQuoteGateway
    {
        IDisposable BegingGetQuote(Guid quoteId, CurrencyPair currencyPair, decimal notional);
        IDisposable BegingAcceptQuote(Guid quoteId);
        IDisposable BegingRejectQuote(Guid quoteId);
    }
}