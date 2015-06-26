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
using Esp.Net.Model;

namespace Esp.Net.Reactive
{
    public class EventSubject<T> : IEventObservable<T>, IEventObserver<T>
    {
        readonly List<IEventObserver<T>> _observers = new List<IEventObserver<T>>();

        public void OnNext(T item)
        {
            var observers = _observers.ToArray();
            foreach(var observer in observers) 
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
            return EspDisposable.Create(() => _observers.Remove(observer));
        }
    }
}