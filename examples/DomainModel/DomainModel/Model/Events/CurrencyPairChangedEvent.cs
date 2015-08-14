namespace Esp.Net.Examples.ComplexModel.Model.Events
{
    public class CurrencyPairChangedEvent
    {
        public CurrencyPairChangedEvent(string currencyPair)
        {
            CurrencyPair = currencyPair;
        }

        public string CurrencyPair { get; private set; }
    }
}