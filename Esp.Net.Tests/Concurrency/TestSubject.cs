using System;
using System.Collections.Generic;
using Esp.Net.Model;

namespace Esp.Net.Concurrency
{
    public class TestSubject<T> : IObservable<T>, IObserver<T>
    {
        public TestSubject()
        {
            Observers = new List<IObserver<T>>();
        }

        public List<IObserver<T>> Observers { get; private set; }

        public void OnNext(T item)
        {
            foreach (IObserver<T> observer in Observers.ToArray())
            {
                observer.OnNext(item);
            }
        }

        public void OnError(Exception error)
        {
            foreach (IObserver<T> observer in Observers.ToArray())
            {
                observer.OnError(error);
            }
        }

        public void OnCompleted()
        {
            foreach (IObserver<T> observer in Observers.ToArray())
            {
                observer.OnCompleted();
            }
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            Observers.Add(observer);
            return EspDisposable.Create(() => Observers.Remove(observer));
        }
    }
}