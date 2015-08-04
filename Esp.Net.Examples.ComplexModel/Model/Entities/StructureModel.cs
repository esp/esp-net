using System;
using Esp.Net.Examples.ComplexModel.Model.Entities.ReferenceData;

namespace Esp.Net.Examples.ComplexModel.Model.Entities
{
    internal class StructureModel
    {
        private readonly IReferenceDataTask _referenceDataTask;
        private decimal? _notional;
        private string _currencyPair;
        private DateTime[] _holidayDates;
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
            _notional = notional;
        }

        public void SetCurrencyPair(string currencyPair)
        {
            _currencyPair = currencyPair;
            _currencyPairRetrievalStatus = CurrencyPairRetrievalStatus.Requested;
            _referenceDataTask.BeginGetReferenceDataForCurrencyPair(Id, currencyPair);
        }

        public void SetCurrencyPairReferenceData(CurrencyPairReferenceData referenceData)
        {
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
            return string.Format("CcyPair: {0}, Notional:{1}", _currencyPair, _notional);
        }
    }
}