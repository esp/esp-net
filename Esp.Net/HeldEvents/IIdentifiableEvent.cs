using System;

namespace Esp.Net.HeldEvents
{
    public interface IIdentifiableEvent
    {
        Guid Id { get; }
    }
}