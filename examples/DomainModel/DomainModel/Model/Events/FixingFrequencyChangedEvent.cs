using Esp.Net.Examples.ComplexModel.Model.Schedule;

namespace Esp.Net.Examples.ComplexModel.Model.Events
{
    public class FixingFrequencyChangedEvent
    {
        public FixingFrequencyChangedEvent(FixingFrequency? frequency)
        {
            Frequency = frequency;
        }

        public FixingFrequency? Frequency { get; private set; }
    }
}