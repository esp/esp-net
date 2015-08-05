using System;
using System.Reactive.Concurrency;
using System.Threading;

namespace Esp.Net.Examples.ComplexModel
{
    public class RouterScheduler : IThreadGuard, IScheduler
    {
        private readonly EventLoopScheduler _modelScheduler;
        
        private readonly string _threadName;

        public RouterScheduler()
        {
            _threadName = "RouterThread";
            _modelScheduler = new EventLoopScheduler(ts => new Thread(ts) { Name = _threadName});
        }

        public bool CheckAccess()
        {
            return Thread.CurrentThread.Name == _threadName;
        }

        public IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action)
        {
            return _modelScheduler.Schedule(state, action);
        }

        public IDisposable Schedule<TState>(TState state, TimeSpan dueTime, Func<IScheduler, TState, IDisposable> action)
        {
            return _modelScheduler.Schedule(state, dueTime, action);

        }

        public IDisposable Schedule<TState>(TState state, DateTimeOffset dueTime, Func<IScheduler, TState, IDisposable> action)
        {
            return _modelScheduler.Schedule(state, dueTime, action);
        }

        public DateTimeOffset Now
        {
            get { return _modelScheduler.Now; }
        }
    }
}