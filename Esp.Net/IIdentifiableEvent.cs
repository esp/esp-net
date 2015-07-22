using System;

#if ESP_EXPERIMENTAL
namespace Esp.Net
{
    public interface IIdentifiableEvent
    {
        Guid Id { get; }
    }
}
#endif