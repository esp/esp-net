using System;

namespace Esp.Net.Reactive
{
    public interface IEventObservable<out T>
    {
        IDisposable Observe(Action<T> onNext);
        IDisposable Observe(IEventObserver<T> observer);
    }

    public static class EventObservable
    {
        public static IEventObservable<T> Create<T>(Func<IEventObserver<T>, IDisposable> subscribe)
        {
            return new EventObservable<T>(subscribe);
        }

        public static IEventObservable<T> Concat<T>(params IEventObservable<T>[] sources)
        {
            return Create<T>(
                o =>
                {
                    var disposables = new DisposableCollection();
                    foreach (IEventObservable<T> source in sources)
                    {
                        disposables.Add(source.Observe(o));
                    }
                    return disposables;
                }
            );
        }

        public static IEventObservable<T> Where<T>(this IEventObservable<T> source, Func<T, bool> predicate)
        {
            return Create<T>(
                o =>
                {
                    var disposable = source.Observe(
                        i =>
                        {
                            if (predicate(i))
                            {
                                o.OnNext(i);
                            }
                        }
                    );
                    return disposable;
                }
            );
        }
    }

    public class EventObservable<T> : IEventObservable<T>
    {
        private readonly Func<IEventObserver<T>, IDisposable> _subscribe;

        public EventObservable(Func<IEventObserver<T>, IDisposable> subscribe)
        {
            _subscribe = subscribe;
        }

        public IDisposable Observe(Action<T> onNext)
        {
            var streamObserver = new EventObserver<T>(onNext);
            return Observe(streamObserver);
        }

        public IDisposable Observe(IEventObserver<T> observer)
        {
            return _subscribe(observer);
        }
    }
}