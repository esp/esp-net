#region copyright
// Copyright 2015 Keith Woods
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using System;
using System.Collections.Generic;
using Esp.Net.Disposables;
using Esp.Net.Meta;

namespace Esp.Net.Reactive
{
    internal class EventSubject<TModel, TEvent, TContext> : IEventObservable<TModel, TEvent, TContext>, IEventObserver<TModel, TEvent, TContext>
    {
        private readonly IEventObservationRegistrar _observationRegistrar;
        private readonly List<IEventObserver<TModel, TEvent, TContext>> _observers = new List<IEventObserver<TModel, TEvent, TContext>>();

        public EventSubject(IEventObservationRegistrar observationRegistrar)
        {
            _observationRegistrar = observationRegistrar;
        }

        public void OnNext(TModel model, TEvent @event, TContext context)
        {
            var observers = _observers.ToArray();
            foreach(var observer in observers) 
			{
                observer.OnNext(model, @event, context);
			}
        }

        public IDisposable Observe(ObserveAction<TModel, TEvent> onNext)
        {
            var observer = new EventObserver<TModel, TEvent, TContext>(onNext);
            return Observe(observer);
        }

        public IDisposable Observe(ObserveAction<TModel, TEvent, TContext> onNext)
        {
            var observer = new EventObserver<TModel, TEvent, TContext>(onNext);
            return Observe(observer);
        }

        public IDisposable Observe(IEventObserver<TModel, TEvent, TContext> observer)
        {
            _observers.Add(observer);
            _observationRegistrar.IncrementRegistration<TEvent>();
            return EspDisposable.Create(() =>
            {
                _observers.Remove(observer);
                _observationRegistrar.DecrementRegistration<TEvent>();
            });
        }
    }
}