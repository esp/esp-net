using System;

namespace Esp.Net.Examples.ComplexModel.Model.Snapshot.Schedule
{
    public class CouponSnapshot
    {
        public CouponSnapshot(Guid id, decimal? notional, DateTime? fixingDate, DateTime[] holidayDates)
        {
            Id = id;
            Notional = notional;
            FixingDate = fixingDate;
            HolidayDates = holidayDates;
        }

        public Guid Id { get; private set; }

        public decimal? Notional { get; private set; }
        
        public DateTime? FixingDate { get; private set; }

        public DateTime[] HolidayDates { get; private set; }
    }
}