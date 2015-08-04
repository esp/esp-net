using System;
using Esp.Net.Examples.ComplexModel.Model.Entities.ReferenceData;

namespace Esp.Net.Examples.ComplexModel.Model.Entities
{
    public class StructureModel
    {
        private readonly IReferenceDataTask _referenceDataTask;
        private decimal? _notional;
        private string _currencyPair;
        private DateTime[] _holidayDates = new DateTime[0];
        private readonly Schedule.Schedule _schedule = new Schedule.Schedule();
        private CurrencyPairRetrievalStatus _currencyPairRetrievalStatus;

        public StructureModel(IReferenceDataTask referenceDataTask)
        {
            Id = Guid.NewGuid();
            _referenceDataTask = referenceDataTask;
        }

        public Guid Id { get; private set; }

        public void SetNotional(decimal? notional)
        {
            Console.WriteLine("MODEL: Setting notional pair to {0}", notional);
            _notional = notional;
        }

        public void SetCurrencyPair(string currencyPair)
        {
            Console.WriteLine("MODEL: Setting currency pair to {0}", currencyPair);
            _currencyPair = currencyPair;
            _currencyPairRetrievalStatus = CurrencyPairRetrievalStatus.Requested;
            _referenceDataTask.BeginGetReferenceDataForCurrencyPair(Id, currencyPair);
        }

        public void SetCurrencyPairReferenceData(CurrencyPairReferenceData referenceData)
        {
            Console.WriteLine("MODEL: Setting ref data");
            _currencyPairRetrievalStatus = CurrencyPairRetrievalStatus.Received;
            _holidayDates = referenceData.HolidayDates;
            _schedule.SetReferenceData(referenceData);
        }

        private enum CurrencyPairRetrievalStatus
        {
            None,
            Requested,
            Received
            // TODO Error, Timeout
        }

        public override string ToString()
        {
            return string.Format("CcyPair: {0}, Notional:{1}, HolidayDateCount:{2}", _currencyPair, _notional, _holidayDates.Length);
        }
    }
}