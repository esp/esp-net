using System;

namespace Esp.Net.Examples.ComplexModel.Model.Snapshot.Schedule
{
    public class CouponSnapshot
    {
        public CouponSnapshot(decimal? notional, Guid id)
        {
            Notional = notional;
            Id = id;
        }

        public Guid Id { get; private set; }

        public decimal? Notional { get; private set; }
    }
}