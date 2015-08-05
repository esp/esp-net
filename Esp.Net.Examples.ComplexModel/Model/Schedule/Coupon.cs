using System;
using Esp.Net.Examples.ComplexModel.Model.Snapshot.Schedule;

namespace Esp.Net.Examples.ComplexModel.Model.Schedule
{
    public class Coupon
    {
        public decimal? Notional { get; set; }

        public Guid Id { get; set; }

        public CouponSnapshot CreateShapshot()
        {
            return new CouponSnapshot(Notional, Id);
        }
    }
}