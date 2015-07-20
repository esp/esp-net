#if ESP_EXPERIMENTAL

namespace Esp.Net.HeldEvents
{
    public interface IHeldEventStore
    {
        void AddHeldEventDescription(IEventDescription description);
        void RemoveHeldEventDescription(IEventDescription description);
    }
}
#endif