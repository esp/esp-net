using System;
using System.Collections.Generic;

namespace Esp.Net.Meta
{
    public interface IEventsObservationRegistrar
    {
        int GetEventObservationCount<TEventType>(Guid modelId);
        int GetEventObservationCount(Guid modelId, Type eventType);
        IList<EventObservations> GetEventObservations(Guid modelId);
    }
}