using System;

namespace Esp.Net.Pipeline.ReactiveBridge
{
    public static  class ObservableExt
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

    public class EspObserver<T> : IObserver<T>
    {
        private readonly Action<T> _observer;
        private readonly Action<Exception> _onError;
        private readonly Action _onCompleted;

        public EspObserver(Action<T> observer)
            : this(observer, null, null)
        {
            _observer = observer;
        }

        public EspObserver(Action<T> observer, Action<Exception> onError)
            : this(observer, onError, null)
        {
        }

        public EspObserver(Action<T> observer, Action<Exception> onError, Action onCompleted)
        {
            _observer = observer;
            _onError = onError;
            _onCompleted = onCompleted;
        }

        public void OnNext(T value)
        {
            _observer(value);
        }

        public void OnError(Exception error)
        {
            if (_onError != null) _onError(error);
            else
            {
                throw error;
            }
        }

        public void OnCompleted()
        {
            if (_onCompleted != null) _onCompleted();
        }
    }
}