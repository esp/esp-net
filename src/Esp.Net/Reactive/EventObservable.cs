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
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Esp.Net
{
    public interface IEventObservable<out TEvent, out TContext, out TModel>
    {
        IDisposable Observe(Action<TEvent> onNext);
        IDisposable Observe(Action<TEvent> onNext, Action onCompleted);

        IDisposable Observe(Action<TEvent, TContext> onNext);
        IDisposable Observe(Action<TEvent, TContext> onNext, Action onCompleted);

        IDisposable Observe(Action<TEvent, TContext, TModel> onNext);
        IDisposable Observe(Action<TEvent, TContext, TModel> onNext, Action onCompleted);
        
        IDisposable Observe(IEventObserver<TEvent, TContext, TModel> observer);
    }

    public static class EventObservable
    {
        public static IEventObservable<TEvent, TContext, TModel> Create<TEvent, TContext, TModel>(Func<IEventObserver<TEvent, TContext, TModel>, IDisposable> subscribe)
        {
            return new EventObservable<TEvent, TContext, TModel>(subscribe);
        }

        public static IEventObservable<TEvent, TContext, TModel> Create<TEvent, TContext, TModel>(Func<IEventObserver<TEvent, TContext, TModel>, Action> subscribe)
        {
            Func<IEventObserver<TEvent, TContext, TModel>, IDisposable> subscribe1 = o => EspDisposable.Create(subscribe(o));
            return new EventObservable<TEvent, TContext, TModel>(subscribe1);
        }

        public static IEventObservable<TEvent, TContext, TModel> Merge<TEvent, TContext, TModel>(params IEventObservable<TEvent, TContext, TModel>[] sources)
        {
            return MergeInternal(sources);
        }

        public static IEventObservable<TEvent, TContext, TModel> Merge<TEvent, TContext, TModel>(IEnumerable<IEventObservable<TEvent, TContext, TModel>> sources)
        {
            return MergeInternal(sources);
        }

        private static IEventObservable<TEvent, TContext, TModel> MergeInternal<TEvent, TContext, TModel>(IEnumerable<IEventObservable<TEvent, TContext, TModel>> sources)
        {
            return Create<TEvent, TContext, TModel>(
                o =>
                {
                    var disposables = new CollectionDisposable();
                    foreach (IEventObservable<TEvent, TContext, TModel> source in sources)
                    {
                        disposables.Add(source.Observe(o));
                    }
                    return disposables;
                }
            );
        }

        public static IEventObservable<TEvent, TContext, TModel> Where<TEvent, TContext, TModel>(
            this IEventObservable<TEvent, TContext, TModel> source, 
            Func<TEvent, TModel, bool> predicate)
        {
            return Where(source, (e, c, m) => predicate(e, m));
        }

        public static IEventObservable<TEvent, TContext, TModel> Where<TEvent, TContext, TModel>(
            this IEventObservable<TEvent, TContext, TModel> source,
            Func<TEvent, TContext, bool> predicate)
        {
            return Where(source, (e, c, m) => predicate(e, c));
        }

        public static IEventObservable<TEvent, TContext, TModel> Where<TEvent, TContext, TModel>(
            this IEventObservable<TEvent, TContext, TModel> source,
            Func<TEvent, bool> predicate)
        {
            return Where(source, (e, c, m) => predicate(e));
        }

        public static IEventObservable<TEvent, TContext, TModel> Where<TEvent, TContext, TModel>(this IEventObservable<TEvent, TContext, TModel> source, Func<TEvent, TContext, TModel, bool> predicate)
        {
            return Create<TEvent, TContext, TModel>(
                o =>
                {
                    var disposable = source.Observe(
                        (e, c, m) =>
                        {
                            if (predicate(e, c, m))
                            {
                                o.OnNext(e, c, m);
                            }
                        },
                        o.OnCompleted
                        );
                    return disposable;
                }
            );
        }

        public static IEventObservable<TEvent, TContext, TModel> Take<TEvent, TContext, TModel>(this IEventObservable<TEvent, TContext, TModel> source, int number)
        {
            return Create<TEvent, TContext, TModel>(
                o =>
                {
                    int count = 0;
                    IDisposable disposable = null;
                    disposable = source.Observe(
                        (e, c, m) =>
                        {
                            count++;
                            if (count <= number)
                            {
                                o.OnNext(e, c, m);
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

        public static IEventObservable<TEvent, TContext, TSubModel> Select<TEvent, TContext, TModel, TSubModel>(this IEventObservable<TEvent, TContext, TModel> source, Func<TModel, TSubModel> subModelSelector)
        {
            return Create<TEvent, TContext, TSubModel>(
                o =>
                {
                    return source.Observe(
                        (e, c, m) => o.OnNext(e, c, subModelSelector(m)),
                        o.OnCompleted
                        );
                }
            );
        }

        public static IEventObservable<TOtherEvent, TContext, TModel> Cast<TEvent, TContext, TModel, TOtherEvent>(
            this IEventObservable<TEvent, TContext, TModel> source)
            where TOtherEvent : TEvent
        {
            return Create<TOtherEvent, TContext, TModel>(
                o =>
                {
                    return source.Observe(
                        (e, c, m) =>
                        {
                            o.OnNext((TOtherEvent)e, c, m);
                        },
                        o.OnCompleted
                    );
                }
            );
        }
    }

    internal class EventObservable<TEvent, TContext, TModel> : IEventObservable<TEvent, TContext, TModel>
    {
        private readonly Func<IEventObserver<TEvent, TContext, TModel>, IDisposable> _subscribe;

        public EventObservable(Func<IEventObserver<TEvent, TContext, TModel>, IDisposable> subscribe)
        {
            _subscribe = subscribe;
        }

        public IDisposable Observe(Action<TEvent> onNext)
        {
            var streamObserver = new EventObserver<TEvent, TContext, TModel>(onNext);
            return Observe(streamObserver);
        }

        public IDisposable Observe(Action<TEvent> onNext, Action onCompleted)
        {
            var streamObserver = new EventObserver<TEvent, TContext, TModel>(onNext, onCompleted);
            return Observe(streamObserver);
        }

        public IDisposable Observe(Action<TEvent, TContext> onNext)
        {
            var streamObserver = new EventObserver<TEvent, TContext, TModel>(onNext);
            return Observe(streamObserver);
        }

        public IDisposable Observe(Action<TEvent, TContext> onNext, Action onCompleted)
        {
            var streamObserver = new EventObserver<TEvent, TContext, TModel>(onNext, onCompleted);
            return Observe(streamObserver);
        }

        public IDisposable Observe(Action<TEvent, TContext, TModel> onNext)
        {
            var streamObserver = new EventObserver<TEvent, TContext, TModel>(onNext);
            return Observe(streamObserver);
        }

        public IDisposable Observe(Action<TEvent, TContext, TModel> onNext, Action onCompleted)
        {
            var streamObserver = new EventObserver<TEvent, TContext, TModel>(onNext, onCompleted);
            return Observe(streamObserver);
        }

        public IDisposable Observe(IEventObserver<TEvent, TContext, TModel> observer)
        {
            return _subscribe(observer);
        }
    }
}