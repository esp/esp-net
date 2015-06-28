using System;
using Esp.Net.Model;

namespace Esp.Net.Reactive
{
    public class StubIEventObservable<T> : IEventObservable<T, int, IEventContext>
    {
        public bool IsDisposed { get; private set; }
        public bool IsObserved { get; set; }

        public IDisposable Observe(Action<T, int> onNext)
        {
            return EspDisposable.Create(() => IsDisposed = true);
        }

        public IDisposable Observe(Action<T, int, IEventContext> onNext)
        {
            return EspDisposable.Create(() => IsDisposed = true);
        }

        public IDisposable Observe(IEventObserver<T, int, IEventContext> observer)
        {
            return EspDisposable.Create(() => IsDisposed = true);
        }
    }
}