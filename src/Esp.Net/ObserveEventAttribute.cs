using System;

namespace Esp.Net
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ObserveEventAttribute : Attribute
    {
        public ObserveEventAttribute(Type eventType) : this(eventType, ObservationStage.Normal)
        {
        }

        public ObserveEventAttribute(Type eventType, ObservationStage stage)
        {
            EventType = eventType;
            Stage = stage;
        }

        public Type EventType { get; private set; }

        public ObservationStage Stage { get; private set; }
    }
}