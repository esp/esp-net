#if ESP_EXPERIMENTAL

namespace Esp.Net.Plugins.HeldEvents
{
    public interface IHeldEventStore
    {
        void AddHeldEventDescription(IEventDescription description);
        void RemoveHeldEventDescription(IEventDescription description);
    }
}
#endif