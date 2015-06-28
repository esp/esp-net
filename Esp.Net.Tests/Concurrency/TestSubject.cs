using System;
using System.Collections.Generic;
using Esp.Net.Model;

namespace Esp.Net.Concurrency
{
    public class TestSubject<T> : IObservable<T>, IObserver<T>
    {
        private readonly List<IObserver<T>> _observers = new List<IObserver<T>>();

        public void OnNext(T item)
        {
            foreach (IObserver<T> observer in _observers.ToArray())
            {
                observer.OnNext(item);
            }
        }

        public void OnError(Exception error)
        {
            foreach (IObserver<T> observer in _observers.ToArray())
            {
                observer.OnError(error);
            }
        }

        public void OnCompleted()
        {
            foreach (IObserver<T> observer in _observers.ToArray())
            {
                observer.OnCompleted();
            }
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            _observers.Add(observer);
            return EspDisposable.Create(() => _observers.Remove(observer));
        }
    }
}