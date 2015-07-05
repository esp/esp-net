using System;

#if ESP_EXPERIMENTAL
namespace Esp.Net.Model
{
    public interface IIdentifiableEvent
    {
        Guid Id { get; }
    }
}
#endif