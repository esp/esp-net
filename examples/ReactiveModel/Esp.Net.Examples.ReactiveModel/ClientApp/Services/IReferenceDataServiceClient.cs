using System;
using Esp.Net.Examples.ReactiveModel.ClientApp.Services.Entities;

namespace Esp.Net.Examples.ReactiveModel.ClientApp.Services
{
    public interface IReferenceDataServiceClient
    {
        IObservable<CurrencyPair[]> GetCurrencyPairs();
    }
}