#region copyright
// Copyright 2015 Dev Shop Limited
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

// ReSharper disable once CheckNamespace
namespace Esp.Net
{
    public interface IEventObserver<in TEvent, in TContext, in TModel>
    {
        void OnNext(TEvent @event, TContext context, TModel model);
        void OnCompleted();
    }

    internal class EventObserver<TEvent, TContext, TModel> : IEventObserver<TEvent, TContext, TModel>
    {
        private readonly Action _onCompleted;
        private readonly Action<TEvent, TContext, TModel> _onNext;

        public EventObserver(Action<TEvent> onNext)
            : this(onNext, null)
        {
        }

        public EventObserver(Action<TEvent> onNext, Action onCompleted)
            : this((e, c, m) => onNext(e), onCompleted)
        {
        }

        public EventObserver(Action<TEvent, TContext> onNext)
            : this(onNext, null)
        {
        }

        public EventObserver(Action<TEvent, TContext> onNext, Action onCompleted)
            : this((e, c, m) => onNext(e, c), onCompleted)
        {
        }

        public EventObserver(Action<TEvent, TModel> onNext)
            : this(onNext, null)
        {
        }

        public EventObserver(Action<TEvent, TModel> onNext, Action onCompleted)
            : this((e, c, m) => onNext(e, m), onCompleted)
        {
        }

        public EventObserver(Action<TEvent, TContext, TModel> onNext)
            : this(onNext, null)
        {
        }

        public EventObserver(Action<TEvent, TContext, TModel> onNext, Action onCompleted)
        {
            _onNext = onNext;
            _onCompleted = onCompleted;
        }

        public void OnNext(TEvent @event, TContext context, TModel model)
        {
            _onNext(@event, context, model);
        }

        public void OnCompleted()
        {
            if (_onCompleted != null) _onCompleted();
        }
    }
}