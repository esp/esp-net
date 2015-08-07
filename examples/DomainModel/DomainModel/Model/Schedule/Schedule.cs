﻿using System;
using System.Collections.Generic;
using System.Linq;
using Esp.Net.Examples.ComplexModel.Model.Snapshot.Schedule;

namespace Esp.Net.Examples.ComplexModel.Model.Schedule
{
    public class Schedule
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(Schedule));

        private readonly List<Coupon> _coupons = new List<Coupon>();
        private bool _isValid;
        private DateTime[] _holidayDates;

        public bool HasSchedule { get; private set; }

        public void SetHolidayDates(DateTime[] holidayDates)
        {
            Log.Debug("Setting holiday dates");
            _holidayDates = holidayDates;
            foreach (Coupon coupon in _coupons)
            {
                coupon.SetHolidayDates(_holidayDates.ToArray());
            }
        }

        public void AddScheduleCoupons(CouponSnapshot[] coupons)
        {
            Log.Debug("Adding Coupons");
            foreach (CouponSnapshot snapshot in coupons)
            {
                _coupons.Add(new Coupon(snapshot));
            }
            HasSchedule = true;
        }

        public void SetNotionalPerFixing(decimal? notionalPerFixing)
        {
            foreach (Coupon coupon in _coupons)
            {
                coupon.SetNotional(notionalPerFixing);
            }
        }

        public void Reset()
        {
            Log.Debug("Resetting schedule");
            _coupons.Clear();
        }

        public bool Validate()
        {
            Log.Debug("Validating schedule");
            _isValid = _coupons.Count > 0;
            return _isValid;
        }

        public ScheduleSnapshot CreateSnapshot()
        {
            return new ScheduleSnapshot(_coupons.Select(c => c.CreateShapshot()).ToArray());
        }
    }
}