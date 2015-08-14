namespace Esp.Net.Examples.ComplexModel.Model.Snapshot.Schedule.Strategy.Legs
{
    public class LegSnapshot
    {
        public LegSnapshot(decimal? notional, Side side)
        {
            Notional = notional;
            Side = side;
        }

        public decimal? Notional { get; private set; }

        public Side Side { get; private set; } 
    }
}