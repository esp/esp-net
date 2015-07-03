using System;

#if ESP_EXPERIMENTAL
namespace Esp.Net.HeldEvents
{
    public interface IIdentifiableEvent
    {
        Guid Id { get; }
    }
}
#endif