using System;

namespace Esp.Net.Examples.ComplexModel.Model.Entities.ReferenceData
{
    internal class CurrencyPairReferenceData
    {
        public CurrencyPairReferenceData(string currencyPair, DateTime[] holidayDates)
        {
            CurrencyPair = currencyPair;
            HolidayDates = holidayDates;
        }

        public string CurrencyPair { get; private set; }

        public DateTime[] HolidayDates { get; private set; }
    }
}