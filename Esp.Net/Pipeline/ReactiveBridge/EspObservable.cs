using System;
using Esp.Net.Reactive;

namespace Esp.Net.Pipeline.ReactiveBridge
{
    public class EspObservable
    {
        public static IObservable<T> Create<T>(Func<IObserver<T>, IDisposable> subscribe)
        {
            return new EspObservable<T>(subscribe);
        }

        public static IObservable<T> Create<T>(Func<IObserver<T>, Action> subscribe)
        {
            Func<IObserver<T>, IDisposable> subscribe1 = o => EspDisposable.Create(subscribe(o));
            return new EspObservable<T>(subscribe1);
        }
    }

    public class EspObservable<T> : IObservable<T>
    {
        private readonly Func<IObserver<T>, IDisposable> _subscribe;

        public EspObservable(Func<IObserver<T>, IDisposable> subscribe)
        {
            _subscribe = subscribe;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return _subscribe(observer);
        }
    }
}