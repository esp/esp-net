using System;
using System.Collections.Generic;
using System.Linq;
using Esp.Net.Examples.ComplexModel.Model.Snapshot.Schedule.Strategy;
using Esp.Net.Examples.ComplexModel.Model.Strategies.Legs;

namespace Esp.Net.Examples.ComplexModel.Model.Strategies
{
    public class Strategy
    {
        private readonly StrategyType _type;

        protected List<Leg> Legs { get; set; }

        public Strategy(StrategyType type)
        {
            _type = type;

            if (_type == StrategyType.Vanilla)
            {
                Legs = new List<Leg>
                {
                    new Leg()
                };
            }
            else if (_type == StrategyType.Straddle)
            {
                Legs = new List<Leg>
                {
                    new Leg(),
                    new Leg()
                };
            }
        }

        public void SetNotional(decimal? notional)
        {
            foreach (Leg leg in Legs)
            {
                leg.SetNotional(notional);
            }
        }

        public void SetSide(int legIndex, Side side)
        {
            if (_type == StrategyType.Vanilla)
            {
                if (legIndex > 0) throw new InvalidOperationException("Invalid leg index");
                Legs[0].SetSide(side);
            }
            else if (_type == StrategyType.Straddle)
            {
                // if you buy one leg you must sell the other 
                Side otherSide = side == Side.Buy
                    ? Side.Sell
                    : Side.Buy;
                if (legIndex > 1) throw new InvalidOperationException("Invalid leg index");
                if (legIndex == 0)
                {
                    Legs[0].SetSide(side);
                    Legs[1].SetSide(otherSide);
                }
                else if (legIndex == 1)
                {
                    Legs[0].SetSide(otherSide);
                    Legs[1].SetSide(side);
                }
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public StrategySnapshot CreateSnapShot()
        {
            return new StrategySnapshot(_type, Legs.Select(l => l.CreateSnapshot()).ToList());
        }
    }
}