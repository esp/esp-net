using System;

namespace Esp.Net.HeldEvents
{
    public interface IEventDescription
    {
        Guid EventId { get; }
        string Description { get; }
    }
}