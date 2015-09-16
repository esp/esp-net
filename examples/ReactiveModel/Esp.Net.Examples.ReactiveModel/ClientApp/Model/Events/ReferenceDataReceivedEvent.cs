using Esp.Net.Examples.ReactiveModel.ClientApp.Services.Entities;

namespace Esp.Net.Examples.ReactiveModel.ClientApp.Model.Events
{
    public class ReferenceDataReceivedEvent
    {
        public ReferenceDataReceivedEvent(CurrencyPair[] currencyPairs)
        {
            CurrencyPairs = currencyPairs;
        }

        public CurrencyPair[] CurrencyPairs { get; private set; }
    }
}