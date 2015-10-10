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
using Esp.Net.Meta;

// ReSharper disable once CheckNamespace
namespace Esp.Net
{
    internal class EventSubject<TEvent, TContext, TModel> : IEventObservable<TEvent, TContext, TModel>, IEventObserver<TEvent, TContext, TModel>
    {
        private readonly IEventObservationRegistrar _observationRegistrar;
        private readonly List<IEventObserver<TEvent, TContext, TModel>> _observers = new List<IEventObserver<TEvent, TContext, TModel>>();
        private readonly object _gate = new object();
        private bool _hasCompleted = false;

        public EventSubject(IEventObservationRegistrar observationRegistrar)
        {
            _observationRegistrar = observationRegistrar;
        }

        public void OnNext(TEvent @event, TContext context, TModel model)
        {
            var observers = _observers.ToArray();
            foreach(var observer in observers) 
			{
                if (_hasCompleted) break;
                observer.OnNext(@event, context, model);
			}
        }

        public void OnCompleted()
        {
            if (!_hasCompleted)
            {
                _hasCompleted = true;
                var observers = _observers.ToArray();
                foreach (var observer in observers)
                {
                    observer.OnCompleted();
                }
            }
        }

        public IDisposable Observe(Action<TEvent> onNext)
        {
            var observer = new EventObserver<TEvent, TContext, TModel>(onNext);
            return Observe(observer);
        }

        public IDisposable Observe(Action<TEvent> onNext, Action onCompleted)
        {
            var observer = new EventObserver<TEvent, TContext, TModel>(onNext, onCompleted);
            return Observe(observer);
        }

        public IDisposable Observe(Action<TEvent, TContext> onNext)
        {
            var observer = new EventObserver<TEvent, TContext, TModel>(onNext);
            return Observe(observer);
        }

        public IDisposable Observe(Action<TEvent, TContext> onNext, Action onCompleted)
        {
            var observer = new EventObserver<TEvent, TContext, TModel>(onNext, onCompleted);
            return Observe(observer);
        }

        public IDisposable Observe(Action<TEvent, TContext, TModel> onNext)
        {
            var observer = new EventObserver<TEvent, TContext, TModel>(onNext);
            return Observe(observer);
        }

        public IDisposable Observe(Action<TEvent, TContext, TModel> onNext, Action onCompleted)
        {
            var observer = new EventObserver<TEvent, TContext, TModel>(onNext, onCompleted);
            return Observe(observer);
        }

        public IDisposable Observe(IEventObserver<TEvent, TContext, TModel> observer)
        {
            lock (_gate)
            {
                _observers.Add(observer);
            }
            _observationRegistrar.IncrementRegistration<TEvent>();
            return EspDisposable.Create(() =>
            {
                lock (_gate)
                {
                    _observers.Remove(observer);
                }
                _observationRegistrar.DecrementRegistration<TEvent>();
            });
        }
    }
}