using System;

namespace Esp.Net.Meta
{
    public class EventObservations
    {
        public EventObservations(Type eventType)
        {
            EventType = eventType;
        }

        public EventObservations(Type eventType, int numberOfObservers)
        {
            EventType = eventType;
            NumberOfObservers = numberOfObservers;
        }

        public Type EventType { get; private set; }

        public int NumberOfObservers { get; internal set; }
    }
}