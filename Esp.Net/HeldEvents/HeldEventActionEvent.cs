using System;

#if ESP_EXPERIMENTAL
namespace Esp.Net.HeldEvents
{
    public class HeldEventActionEvent
    {
        public HeldEventActionEvent(Guid eventId, HeldEventAction action)
        {
            Action = action;
            EventId = eventId;
        }

        public HeldEventAction Action { get; private set; }

        public Guid EventId { get; private set; }
    }
}
#endif