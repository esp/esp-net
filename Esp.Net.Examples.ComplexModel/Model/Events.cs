using Esp.Net.Examples.ComplexModel.Model.Entities;
using Esp.Net.Examples.ComplexModel.Model.Entities.ReferenceData;

namespace Esp.Net.Examples.ComplexModel.Model
{
    internal class NotionalChangedEvent
    {
        public NotionalChangedEvent(decimal? notional)
        {
            Notional = notional;
        }

        public decimal? Notional { get; private set; } 
    }

    internal class CurrencyPairChangedEvent
    {
        public CurrencyPairChangedEvent(string currencyPair)
        {
            CurrencyPair = currencyPair;
        }

        public string CurrencyPair { get; private set; }
    }

    internal class CurrencyPairReferenceDataReceivedEvent
    {
        public CurrencyPairReferenceDataReceivedEvent(CurrencyPairReferenceData refData)
        {
            RefData = refData;
        }

        public CurrencyPairReferenceData RefData { get; private set; }
    }
}