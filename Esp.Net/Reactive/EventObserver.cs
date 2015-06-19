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