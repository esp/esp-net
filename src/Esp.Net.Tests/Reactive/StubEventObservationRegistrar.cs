using System;
using System.Collections.Generic;
using Esp.Net.Meta;

namespace Esp.Net.Reactive
{
    public class StubEventObservationRegistrar : IEventObservationRegistrar
    {
        public StubEventObservationRegistrar()
        {
            Register = new Dictionary<Type, int>();
        }

        public Dictionary<Type, int> Register { get; private set; }
          
        public void IncrementRegistration<TEvent>()
        {
            if (Register.ContainsKey(typeof (TEvent)))
            {
                Register[typeof (TEvent)]++;
            }
            else
            {
                Register[typeof(TEvent)] = 1;
            }
        }

        public void DecrementRegistration<TEvent>()
        {
            Register[typeof(TEvent)]--;
        }
    }
}