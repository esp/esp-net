using Esp.Net.Examples.ComplexModel.Model.Schedule;
using Esp.Net.Examples.ComplexModel.Model.Snapshot.Schedule;

namespace Esp.Net.Examples.ComplexModel.Model.Snapshot
{
    // immutable representation of a StructureModel
    public class StructureSnapshot
    {
        public StructureSnapshot(ScheduleSnapshot schedule, bool isValid, FixingFrequency? frequency, int version, string currencyPair, decimal? notional)
        {
            Schedule = schedule;
            IsValid = isValid;
            Frequency = frequency;
            Version = version;
            CurrencyPair = currencyPair;
            Notional = notional;
        }

        public int Version { get; private set; }

        public string CurrencyPair { get; private set; }

        public decimal? Notional { get; private set; }

        public bool IsValid { get; private set; }

        public FixingFrequency? Frequency { get; private set; }

        public ScheduleSnapshot Schedule { get; private set; }

        public override string ToString()
        {
            return string.Format("CcyPair: {0}, Notional:{1}, Version:{2}, CouponCount:{3}", CurrencyPair, Notional, Version, Schedule.Coupons.Count);
        }
    }
}