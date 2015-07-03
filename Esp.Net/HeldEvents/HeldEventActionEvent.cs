using System;

namespace Esp.Net.HeldEvents
{
    public class HeldEventActionEvent
    {
        public HeldEventActionEvent(HeldEventAction action, Guid eventId)
        {
            Action = action;
            EventId = eventId;
        }

        public HeldEventAction Action { get; private set; }

        public Guid EventId { get; private set; }
    }
}