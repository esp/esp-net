using System;

namespace Esp.Net.Meta
{
    public class EventObservations
    {
        public EventObservations(Type eventType)
        {
            EventType = eventType;
        }

        public Type EventType { get; private set; }
        public int NumberOfObservers { get; set; }
    }
}