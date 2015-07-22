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

namespace Esp.Net.Reactive
{
    public interface IEventObserver<in TModel, in TEvent, in TContext>
    {
        void OnNext(TModel model, TEvent @event, TContext context);
    }

    internal class EventObserver<TModel, TEvent, TContext> : IEventObserver<TModel, TEvent, TContext>
    {
        private readonly ObserveAction<TModel, TEvent, TContext> _onNext;

        public EventObserver(ObserveAction<TModel, TEvent> onNext)
        {
            _onNext = (m, e, c) => onNext(m, e);
        }

        public EventObserver(ObserveAction<TModel, TEvent, TContext> onNext)
        {
            _onNext = onNext;
        }

        public void OnNext(TModel model, TEvent @event, TContext context)
        {
            _onNext(model, @event, context);
        }
    }
}