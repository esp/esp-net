using System;
using System.Collections.Generic;

namespace Esp.Net.Meta
{
    public interface IEventsObservationRegistrar
    {
        int GetEventObservationCount<TEventType>(object modelId);
        int GetEventObservationCount(object modelId, Type eventType);
        IList<EventObservations> GetEventObservations(object modelId);
    }
}