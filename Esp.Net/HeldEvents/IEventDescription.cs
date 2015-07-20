#if ESP_EXPERIMENTAL
using System;

namespace Esp.Net.HeldEvents
{
    public interface IEventDescription
    {
        Guid EventId { get; }
        string Category { get; }
        string Description { get; }
    }
}
#endif