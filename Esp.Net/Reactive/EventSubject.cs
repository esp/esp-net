using System;
using System.Collections.Generic;

namespace Esp.Net.Reactive
{
    public class EventSubject<T> : IEventObservable<T>, IEventObserver<T>
    {
        readonly List<IEventObserver<T>> _observers = new List<IEventObserver<T>>();

        public void OnNext(T item)
        {
            foreach(var observer in _observers) 
			{
				observer.OnNext(item);
			}
        }

        public IDisposable Observe(Action<T> onNext)
        {
            var observer = new EventObserver<T>(onNext);
            return Observe(observer);
        }

        public IDisposable Observe(IEventObserver<T> observer)
        {
            _observers.Add(observer);
            return Disposable.Create(() => _observers.Remove(observer));
        }
    }
}