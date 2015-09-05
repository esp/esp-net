using System;
using System.Diagnostics;
using System.Threading;
using Esp.Net;

namespace Exp.Net.PerfTests
{
    class Program
    {
        static void Main(string[] args)
        {
            // very rough spike... much todo 

            var modelId = 1;
            Stopwatch stopwatch = new Stopwatch();
            var eventCount = 10000000;
            var model = new Model(eventCount, stopwatch);
            AutoResetEvent gate = new AutoResetEvent(false);
            Console.WriteLine("Running");
            try
            {
                var r = new Router(new NewThreadRouterDispatcher());
                r.RegisterModel(modelId, model);
                r.GetEventObservable<Model, Event1>(modelId).Observe((m, e) =>
                {
                    m.ReceiveEvent(e);
                    if (e.Index == eventCount - 1) gate.Set();
                });
                Console.WriteLine("About to publish");
                for (int i = 0; i < eventCount; i++)
                {
                    r.PublishEvent(modelId, new Event1(i));
                }
                gate.WaitOne(500);
                Thread.Sleep(TimeSpan.FromMinutes(1)); 
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            Console.WriteLine("Done. Processed {0} events", model.RecevedEventCount);
            Console.ReadKey();
        }

        public class Model
        {
            private readonly Stopwatch _stopwatch;

            public Model(int expectedEvents, Stopwatch stopwatch)
            {
                _stopwatch = stopwatch;
                ReceivedEventTimings = new long[expectedEvents];
            }

            public long[] ReceivedEventTimings { get; private set; }
            public int RecevedEventCount { get; private set; }

            public void ReceiveEvent(Event1 @event)
            {
                RecevedEventCount++;
                ReceivedEventTimings[@event.Index] = _stopwatch.ElapsedTicks;
            }
        }

        public class Event1
        {
            public Event1(int index)
            {
                Index = index;
            }

            public int Index { get; private set; }
        }
    }
}
