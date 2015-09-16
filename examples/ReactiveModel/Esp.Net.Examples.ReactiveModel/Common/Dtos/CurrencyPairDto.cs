namespace Esp.Net.Examples.ReactiveModel.Common.Dtos
{
    public class CurrencyPairDto
    {
        public CurrencyPairDto(string isoCode, int precision)
        {
            IsoCode = isoCode;
            Precision = precision;
        }

        public string IsoCode { get; private set; }

        public int Precision { get; private set; }
    }
}