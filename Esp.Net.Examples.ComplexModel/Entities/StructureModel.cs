using System;

namespace Esp.Net.Examples.ComplexModel.Entities
{
    internal class StructureModel
    {
        private readonly IReferenceData _referenceData;
        private decimal? _notional;
        private string _currencyPair;
        private DateTime[] _holidayDates;
        private readonly Schedule _schedule = new Schedule();

        public StructureModel(IReferenceData referenceData)
        {
            _referenceData = referenceData;

            Id = Guid.NewGuid();
        }

        public Guid Id { get; private set; }

        public void SetNotional(decimal? notional)
        {
            _notional = notional;
        }

        public void SetCurrencyPair(string currencyPair)
        {
            _currencyPair = currencyPair;
        }

        public void SetReferenceData(ReferenceData referenceData)
        {
            _holidayDates = referenceData.HolidayDates;
            _schedule.SetReferenceData(referenceData);
        }
    }

    internal class ReferenceData
    {
        public DateTime[] HolidayDates { get; private set; }
    }

    internal interface IReferenceData
    {
        void GetReferenceData(Guid modelId, string currencyPair);
    }

    internal class Schedule
    {
        public void SetReferenceData(ReferenceData referenceData)
        {

        }

        public Guid AddRow()
        {
            return Guid.NewGuid();
        }
    }
}