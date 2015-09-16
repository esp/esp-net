using System;
using System.Reactive;
using Esp.Net.Examples.ReactiveModel.ClientApp.Services.Entities;

namespace Esp.Net.Examples.ReactiveModel.ClientApp.Services
{
    public interface IRfqServiceClient
    {
        IObservable<RfqResponse> RequestQuote(RfqRequest request);

        IObservable<Unit> AcceptQuote(Guid quoteId);

        IObservable<Unit> RejectQuote(Guid quoteId);
    }
}