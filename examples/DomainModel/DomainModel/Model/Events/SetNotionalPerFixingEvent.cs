namespace Esp.Net.Examples.ComplexModel.Model.Events
{
    public class SetNotionalPerFixingEvent
    {
        public SetNotionalPerFixingEvent(decimal? notionalPerFixing)
        {
            NotionalPerFixing = notionalPerFixing;
        }

        public decimal? NotionalPerFixing { get; private set; } 
    }
}