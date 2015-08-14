using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Esp.Net.Examples.ComplexModel.Model.Snapshot.Schedule
{
    public class ScheduleSnapshot
    {
        public ScheduleSnapshot(CouponSnapshot[] coupons)
        {
            Coupons = new ReadOnlyCollection<CouponSnapshot>(coupons);
        }

        public IList<CouponSnapshot> Coupons { get; private set; }
    }
}