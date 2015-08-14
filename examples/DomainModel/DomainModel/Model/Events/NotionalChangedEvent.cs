namespace Esp.Net.Examples.ComplexModel.Model.Events
{
    public class NotionalChangedEvent
    {
        public NotionalChangedEvent(decimal? notional)
        {
            Notional = notional;
        }

        public decimal? Notional { get; private set; } 
    }
}