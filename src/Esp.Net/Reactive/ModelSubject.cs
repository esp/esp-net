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

// ReSharper disable once CheckNamespace
namespace Esp.Net
{
    internal class ModelSubject<T> : IModelObservable<T>, IModelObserver<T>
    {
        readonly List<IModelObserver<T>> _observers = new List<IModelObserver<T>>();
        private readonly object _gate = new object();
        private bool _hasCompleted = false;

        public void OnNext(T item)
        {
            var observers = _observers.ToArray();
            foreach(var observer in observers) 
			{
                if (_hasCompleted) break;
				observer.OnNext(item);
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

        public IDisposable Observe(Action<T> onNext)
        {
            var observer = new ModelObserver<T>(onNext);
            return Observe(observer);
        }

        public IDisposable Observe(Action<T> onNext, Action onCompleted)
        {
            var observer = new ModelObserver<T>(onNext, onCompleted);
            return Observe(observer);
        }

        public IDisposable Observe(IModelObserver<T> observer)
        {
            lock (_gate)
            {
                _observers.Add(observer);
            }
            return EspDisposable.Create(() =>
            {
                lock (_gate)
                {
                    _observers.Remove(observer);
                }
            });
        }
    }
}