using System;

namespace Esp.Net.HeldEvents
{
    public interface IHeldEventStore
    {
        void AddHeldEventDescription(Guid id, IEventDescription e);
        void RemoveHeldEventDescription(Guid id);
    }
}