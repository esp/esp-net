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
    public interface IModelObservable<out T>
    {
        IDisposable Observe(Action<T> onNext);
        IDisposable Observe(IModelObserver<T> observer);
    }

    public static class ModelObservable
    {
        public static IModelObservable<T> Create<T>(Func<IModelObserver<T>, IDisposable> subscribe)
        {
            return new ModelObservable<T>(subscribe);
        }

        public static IModelObservable<T> Where<T>(this IModelObservable<T> source, Func<T, bool> predicate)
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

        public static IModelObservable<T> Take<T>(this IModelObservable<T> source, int number)
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

    internal class ModelObservable<T> : IModelObservable<T>
    {
        private readonly Func<IModelObserver<T>, IDisposable> _subscribe;

        public ModelObservable(Func<IModelObserver<T>, IDisposable> subscribe)
        {
            _subscribe = subscribe;
        }

        public IDisposable Observe(Action<T> onNext)
        {
            var streamObserver = new ModelObserver<T>(onNext);
            return Observe(streamObserver);
        }

        public IDisposable Observe(IModelObserver<T> observer)
        {
            return _subscribe(observer);
        }
    }
}