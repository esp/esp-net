using Esp.Net.Disposables;

#if ESP_LOCAL
// ReSharper disable once CheckNamespace
namespace System.Reactive.Linq
{
    internal class Observable
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

    internal class EspObservable<T> : IObservable<T>
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
#endif