using Esp.Net.Examples.ReactiveModel.ClientApp.Services.Entities;

namespace Esp.Net.Examples.ReactiveModel.ClientApp.Model.Events
{
    public class CurrencyPairChangedEvent
    {
        public CurrencyPairChangedEvent(CurrencyPair currencyPair)
        {
            CurrencyPair = currencyPair;
        }

        public CurrencyPair CurrencyPair { get; private set; }
    }
}