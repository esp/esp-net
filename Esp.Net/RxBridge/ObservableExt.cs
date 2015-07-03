#if ESP_LOCAL
// ReSharper disable once CheckNamespace
namespace System.Reactive.Linq
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
#endif