using System;

namespace Esp.Net.Reactive
{
    public interface IEventObserver<in T>
    {
        void OnNext(T item);
    }

    public class EventObserver<T> : IEventObserver<T>
    {
        private readonly Action<T> _onNext;

        public EventObserver(Action<T> onNext)
        {
            _onNext = onNext;
        }

        public void OnNext(T item)
        {
            _onNext(item);
        }
    }
}