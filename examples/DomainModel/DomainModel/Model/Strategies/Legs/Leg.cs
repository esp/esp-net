using Esp.Net.Examples.ComplexModel.Model.Snapshot.Schedule.Strategy.Legs;

namespace Esp.Net.Examples.ComplexModel.Model.Strategies.Legs
{
    public class Leg
    {
        private decimal? _notional;
        private Side _side;

        public void SetNotional(decimal? notional)
        {
            _notional = notional;
        }

        public void SetSide(Side side)
        {
            _side = side;
        }

        public LegSnapshot CreateSnapshot()
        {
            return new LegSnapshot(_notional, _side);
        }
    }
}