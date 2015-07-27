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
using Esp.Net.Disposables;

namespace Esp.Net.Reactive
{
    public delegate void ObserveAction<in TModel, in TEvent>(TModel model, TEvent e);
    public delegate void ObserveAction<in TModel, in TEvent, in TContext>(TModel model, TEvent e, TContext context);

    public interface IEventObservable<out TModel, out TEvent, out TContext>
    {
        IDisposable Observe(ObserveAction<TModel, TEvent> onNext);
        IDisposable Observe(ObserveAction<TModel, TEvent> onNext, Action onCompleted);

        IDisposable Observe(ObserveAction<TModel, TEvent, TContext> onNext);
        IDisposable Observe(ObserveAction<TModel, TEvent, TContext> onNext, Action onCompleted);
        
        IDisposable Observe(IEventObserver<TModel, TEvent, TContext> observer);
    }

    public static class EventObservable
    {
        public static IEventObservable<TModel, TEvent, TContext> Create<TModel, TEvent, TContext>(Func<IEventObserver<TModel, TEvent, TContext>, IDisposable> subscribe)
        {
            return new EventObservable<TModel, TEvent, TContext>(subscribe);
        }

        public static IEventObservable<TModel, TEvent, TContext> Create<TModel, TEvent, TContext>(Func<IEventObserver<TModel, TEvent, TContext>, Action> subscribe)
        {
            Func<IEventObserver<TModel, TEvent, TContext>, IDisposable> subscribe1 = o => EspDisposable.Create(subscribe(o));
            return new EventObservable<TModel, TEvent, TContext>(subscribe1);
        }

        public static IEventObservable<TModel, TEvent, TContext> Concat<TModel, TEvent, TContext>(params IEventObservable<TModel, TEvent, TContext>[] sources)
        {
            return Create<TModel, TEvent, TContext>(
                o =>
                {
                    var disposables = new CollectionDisposable();
                    foreach (IEventObservable<TModel, TEvent, TContext> source in sources)
                    {
                        disposables.Add(source.Observe(o));
                    }
                    return disposables;
                }
            );
        }

        public static IEventObservable<TModel, TEvent, TContext> Where<TModel, TEvent, TContext>(
            this IEventObservable<TModel, TEvent, TContext> source, 
            Func<TModel, TEvent, bool> predicate)
        {
            return Where(source, (m, e, c) => predicate(m, e));
        }

        public static IEventObservable<TModel, TEvent, TContext> Where<TModel, TEvent, TContext>(this IEventObservable<TModel, TEvent, TContext> source, Func<TModel, TEvent, TContext, bool> predicate)
        {
            return Create<TModel, TEvent, TContext>(
                o =>
                {
                    var disposable = source.Observe(
                        (m, e, c) =>
                        {
                            if (predicate(m, e, c))
                            {
                                o.OnNext(m, e, c);
                            }
                        },
                        o.OnCompleted
                    );
                    return disposable;
                }
            );
        }

        public static IEventObservable<TModel, TEvent, TContext> Take<TModel, TEvent, TContext>(this IEventObservable<TModel, TEvent, TContext> source, int number)
        {
            return Create<TModel, TEvent, TContext>(
                o =>
                {
                    int count = 0;
                    IDisposable disposable = null;
                    disposable = source.Observe(
                        (m, e, c) =>
                        {
                            count++;
                            if (count <= number)
                            {
                                o.OnNext(m, e, c);
                            }
                            else
                            {
                                disposable.Dispose();
                            }
                        },
                        o.OnCompleted
                    );
                    return disposable;
                }
            );
        }
    }

    internal class EventObservable<TModel, TEvent, TContext> : IEventObservable<TModel, TEvent, TContext>
    {
        private readonly Func<IEventObserver<TModel, TEvent, TContext>, IDisposable> _subscribe;

        public EventObservable(Func<IEventObserver<TModel, TEvent, TContext>, IDisposable> subscribe)
        {
            _subscribe = subscribe;
        }

        public IDisposable Observe(ObserveAction<TModel, TEvent> onNext)
        {
            var streamObserver = new EventObserver<TModel, TEvent, TContext>(onNext);
            return Observe(streamObserver);
        }

        public IDisposable Observe(ObserveAction<TModel, TEvent> onNext, Action onCompleted)
        {
            var streamObserver = new EventObserver<TModel, TEvent, TContext>(onNext, onCompleted);
            return Observe(streamObserver);
        }

        public IDisposable Observe(ObserveAction<TModel, TEvent, TContext> onNext)
        {
            var streamObserver = new EventObserver<TModel, TEvent, TContext>(onNext);
            return Observe(streamObserver);
        }

        public IDisposable Observe(ObserveAction<TModel, TEvent, TContext> onNext, Action onCompleted)
        {
            var streamObserver = new EventObserver<TModel, TEvent, TContext>(onNext, onCompleted);
            return Observe(streamObserver);
        }

        public IDisposable Observe(IEventObserver<TModel, TEvent, TContext> observer)
        {
            return _subscribe(observer);
        }
    }
}