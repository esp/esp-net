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
using Esp.Net.Model;

namespace Esp.Net.Reactive
{
    public interface IEventObservable<out T>
    {
        IDisposable Observe(Action<T> onNext);
        IDisposable Observe(IEventObserver<T> observer);
    }

    public static class EventObservable
    {
        public static IEventObservable<T> Create<T>(Func<IEventObserver<T>, IDisposable> subscribe)
        {
            return new EventObservable<T>(subscribe);
        }

        public static IEventObservable<T> Create<T>(Func<IEventObserver<T>, Action> subscribe)
        {
            Func<IEventObserver<T>, IDisposable> subscribe1 = o => EspDisposable.Create(subscribe(o));
            return new EventObservable<T>(subscribe1);
        }

        public static IEventObservable<T> Concat<T>(params IEventObservable<T>[] sources)
        {
            return Create<T>(
                o =>
                {
                    var disposables = new DisposableCollection();
                    foreach (IEventObservable<T> source in sources)
                    {
                        disposables.Add(source.Observe(o));
                    }
                    return disposables;
                }
            );
        }

        public static IEventObservable<T> Where<T>(this IEventObservable<T> source, Func<T, bool> predicate)
        {
            return Create<T>(
                o =>
                {
                    var disposable = source.Observe(
                        i =>
                        {
                            if (predicate(i))
                            {
                                o.OnNext(i);
                            }
                        }
                    );
                    return disposable;
                }
            );
        }

        public static IEventObservable<T> Take<T>(this IEventObservable<T> source, int number)
        {
            return Create<T>(
                o =>
                {
                    int count = 0;
                    IDisposable disposable = null;
                    disposable = source.Observe(
                        i =>
                        {
                            count++;
                            if (count <= number)
                            {
                                o.OnNext(i);
                            }
                            else
                            {
                                disposable.Dispose();
                            }
                        }
                    );
                    return disposable;
                }
            );
        }
    }

    public class EventObservable<T> : IEventObservable<T>
    {
        private readonly Func<IEventObserver<T>, IDisposable> _subscribe;

        public EventObservable(Func<IEventObserver<T>, IDisposable> subscribe)
        {
            _subscribe = subscribe;
        }

        public IDisposable Observe(Action<T> onNext)
        {
            var streamObserver = new EventObserver<T>(onNext);
            return Observe(streamObserver);
        }

        public IDisposable Observe(IEventObserver<T> observer)
        {
            return _subscribe(observer);
        }
    }
}