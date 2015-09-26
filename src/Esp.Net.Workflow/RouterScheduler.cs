using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Threading;

namespace Esp.Net
{
    public class RouterScheduler
    {
        public RouterScheduler<TModel> Create<TModel>(IRouter router, object modelId)
        {
            
        }
    }
    public class RouterScheduler<TModel> : IScheduler
    {
        private readonly IRouter<TModel> _router;
        private readonly object _modelId;

        public RouterScheduler(IRouter<TModel> router)
        {
            _router = router;
        }

        public IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action)
        {
            int shouldRun = 1;
            // the router could return a disposable here, however it would be inconsistent with how events work, 
            // i.e. PublishEvent doesn't return a disposable. 
            // Given this, event if you dispose the scheduled action, the router will still enter it's dispatch loop and procure a model update (with no change)
            _router.RunAction(() =>
            {
                if (Interlocked.Exchange(ref shouldRun, 0) == 1)
                {
                    action(this, state);
                }
            });
            return Disposable.Create(() => Interlocked.Exchange(ref shouldRun, 0));
        }

        public IDisposable Schedule<TState>(TState state, TimeSpan dueTime, Func<IScheduler, TState, IDisposable> action)
        {
            throw new NotSupportedException();
        }

        public IDisposable Schedule<TState>(TState state, DateTimeOffset dueTime, Func<IScheduler, TState, IDisposable> action)
        {
            throw new NotSupportedException();
        }

        public DateTimeOffset Now { get { return DateTimeOffset.Now; } }
    }
}