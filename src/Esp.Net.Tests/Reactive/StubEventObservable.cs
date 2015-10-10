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

namespace Esp.Net.Reactive
{
    public class StubEventObservable<TModel> : IEventObservable<int, IEventContext, TModel>
    {
        public bool IsDisposed { get; private set; }
        public bool IsObserved { get; set; }

        public IDisposable Observe(Action<int> onNext)
        {
            return EspDisposable.Create(() => IsDisposed = true);
        }

        public IDisposable Observe(Action<int> onNext, Action onCompleted)
        {
            return EspDisposable.Create(() => IsDisposed = true);
        }

        public IDisposable Observe(Action<int, IEventContext> onNext)
        {
            return EspDisposable.Create(() => IsDisposed = true);
        }

        public IDisposable Observe(Action<int, IEventContext> onNext, Action onCompleted)
        {
            return EspDisposable.Create(() => IsDisposed = true);
        }

        public IDisposable Observe(Action<int, IEventContext, TModel> onNext)
        {
            return EspDisposable.Create(() => IsDisposed = true);
        }

        public IDisposable Observe(Action<int, IEventContext, TModel> onNext, Action onCompleted)
        {
            return EspDisposable.Create(() => IsDisposed = true);
        }

        public IDisposable Observe(IEventObserver<int, IEventContext, TModel> observer)
        {
            return EspDisposable.Create(() => IsDisposed = true);
        }
    }
}