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
    public interface IEventObserver<in TModel, in TEvent, in TContext>
    {
        void OnNext(TModel model, TEvent @event, TContext context);
        void OnCompleted();
    }

    internal class EventObserver<TModel, TEvent, TContext> : IEventObserver<TModel, TEvent, TContext>
    {
        private readonly Action _onCompleted;
        private readonly Action<TModel, TEvent, TContext> _onNext;

        public EventObserver(Action<TModel, TEvent> onNext)
            : this(onNext, null)
        {
        }

        public EventObserver(Action<TModel, TEvent> onNext, Action onCompleted)
            : this((m, e, c) => onNext(m, e), onCompleted)
        {
        }

        public EventObserver(Action<TModel, TEvent, TContext> onNext)
            : this(onNext, null)
        {
        }

        public EventObserver(Action<TModel, TEvent, TContext> onNext, Action onCompleted)
        {
            _onNext = onNext;
            _onCompleted = onCompleted;
        }

        public void OnNext(TModel model, TEvent @event, TContext context)
        {
            _onNext(model, @event, context);
        }

        public void OnCompleted()
        {
            if (_onCompleted != null) _onCompleted();
        }
    }
}