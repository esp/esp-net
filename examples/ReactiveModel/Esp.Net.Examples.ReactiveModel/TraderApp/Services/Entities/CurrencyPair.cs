namespace Esp.Net.Examples.ReactiveModel.TraderApp.Services.Entities
{
    public class CurrencyPair
    {
        public CurrencyPair(string isoCode, int precision)
        {
            IsoCode = isoCode;
            Precision = precision;

            Base = IsoCode.Substring(0, 3);
            Counter = IsoCode.Substring(3, 3);
        }

        public string IsoCode { get; private set; }

        public int Precision { get; private set; }

        public string Base { get; private set; }

        public string Counter { get; private set; }
    }
}