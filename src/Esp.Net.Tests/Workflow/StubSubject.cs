
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

namespace Esp.Net.Reactive
{
    public class StubSubject<T> : IObservable<T>, IObserver<T>
    {
        public StubSubject()
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