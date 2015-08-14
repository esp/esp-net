using System.Collections.Generic;
using System.Collections.ObjectModel;
using Esp.Net.Examples.ComplexModel.Model.Snapshot.Schedule.Strategy.Legs;
using Esp.Net.Examples.ComplexModel.Model.Strategies;

namespace Esp.Net.Examples.ComplexModel.Model.Snapshot.Schedule.Strategy
{
    public class StrategySnapshot
    {
        public StrategySnapshot(StrategyType type, IList<LegSnapshot> legs)
        {
            Type = type;
            Legs = new ReadOnlyCollection<LegSnapshot>(legs);
        }

        public StrategyType Type { get; private set; }

        public IList<LegSnapshot> Legs { get; private set; } 
    }
}