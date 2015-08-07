using Esp.Net.Examples.ComplexModel.Model.Snapshot.Schedule;

namespace Esp.Net.Examples.ComplexModel.Model.Events
{
    public class ScheduleResolvedEvent
    {
        public ScheduleResolvedEvent(CouponSnapshot[] coupons)
        {
            Coupons = coupons;
        }

        public CouponSnapshot[] Coupons { get; private set; } 
    }
}