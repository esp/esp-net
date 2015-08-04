namespace Esp.Net.Examples.ComplexModel.Entities
{
    public class NotionalChangedEvent
    {
        public NotionalChangedEvent(decimal? notional)
        {
            Notional = notional;
        }

        public decimal? Notional { get; private set; } 
    }

    public class CurrencyPairChangedEvent
    {
        public CurrencyPairChangedEvent(string currencyPair)
        {
            CurrencyPair = currencyPair;
        }

        public string CurrencyPair { get; private set; }
    }
}