using System;
using Esp.Net.Examples.ComplexModel.Model.Snapshot.Schedule;

namespace Esp.Net.Examples.ComplexModel.Model.Schedule
{
    public class Coupon
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(Coupon));

        private decimal? _notional;

        private DateTime? _fixingDate;

        private DateTime[] _holidayDates;

        public Coupon(DateTime? fixingDate)
        {
            _fixingDate = fixingDate;
        }

        public Coupon(CouponSnapshot snapshot)
        {
            Id = snapshot.Id;
            _notional = snapshot.Notional;
            _fixingDate = snapshot.FixingDate;
            _holidayDates = snapshot.HolidayDates;
        }

        public Guid Id { get; private set; }

        public void SetFixingDate(DateTime? date)
        {
            Log.DebugFormat("Setting fixing date to {0}", date);
            _fixingDate = date;
        }

        public void SetHolidayDates(DateTime[] dates)
        {
            Log.Debug("Setting holiday dates");
            _holidayDates = dates;
        }

        public void SetNotional(decimal? notional)
        {
            Log.DebugFormat("Setting notional to {0}", notional);
            _notional = notional;
        }

        public CouponSnapshot CreateShapshot()
        {
            return new CouponSnapshot(Id, _notional, _fixingDate, _holidayDates);
        }
    }
}