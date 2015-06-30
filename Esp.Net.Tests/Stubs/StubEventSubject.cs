using System;
using System.Collections.Generic;
using Esp.Net.Model;
using Esp.Net.Reactive;

namespace Esp.Net.Stubs
{
    public class StubEventSubject<TModel, TEvent, TContext> : IEventObservable<TModel, TEvent, TContext>, IEventObserver<TModel, TEvent, TContext>
    {
        public StubEventSubject()
        {
            Observers = new List<Action<TModel, TEvent, TContext>>();
        }

        public List<Action<TModel, TEvent, TContext>> Observers { get; private set; }

        public IDisposable Observe(Action<TModel, TEvent> onNext)
        {
            Action<TModel, TEvent, TContext> action = (m, e, c) => onNext(m, e);
            Observers.Add(action);
            return EspDisposable.Create(() => Observers.Remove(action));
        }

        public IDisposable Observe(Action<TModel, TEvent, TContext> onNext)
        {
            Observers.Add(onNext);
            return EspDisposable.Create(() => Observers.Remove(onNext));
        }

        public IDisposable Observe(IEventObserver<TModel, TEvent, TContext> observer)
        {
            Observers.Add(observer.OnNext);
            return EspDisposable.Create(() => Observers.Remove(observer.OnNext));

        }

        public void OnNext(TModel model, TEvent @event, TContext context)
        {
            foreach (Action<TModel, TEvent, TContext> eventObserver in Observers)
            {
                eventObserver(model, @event, context);
            }
        }
    }
}