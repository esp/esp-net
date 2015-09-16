using System;
using Esp.Net.Examples.ReactiveModel.TraderApp.Services.Entities;

namespace Esp.Net.Examples.ReactiveModel.TraderApp.Services
{
    public interface IRfqService
    {
        void SendUpdate(RfqResponse response);

        IObservable<RfqRequest> QuoteRequests { get; }

        IObservable<Guid> QuoteAccepts { get; }

        IObservable<Guid> QuoteRejects { get; }

        IObservable<Guid> QuotesClosed { get; }
    }
}