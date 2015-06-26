using System;

namespace Esp.Net.RxBridge
{
    public static class ObservableExt
    {
        public static IDisposable Subscribe<T>(this IObservable<T> source, Action<T> observer)
        {
            return source.Subscribe(new EspObserver<T>(observer));
        }

        public static IDisposable Subscribe<T>(this IObservable<T> source, Action<T> observer, Action<Exception> onError)
        {
            return source.Subscribe(new EspObserver<T>(observer, onError));
        }

        public static IDisposable Subscribe<T>(this IObservable<T> source, Action<T> observer, Action<Exception> onError, Action onCompleted)
        {
            return source.Subscribe(new EspObserver<T>(observer, onError, onCompleted));
        }
    }
}