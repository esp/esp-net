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

#if ESP_LOCAL
// ReSharper disable once CheckNamespace
namespace System.Reactive.Linq
{
    internal static class ObservableExt
    {
        public static IDisposable Subscribe<T>(this IObservable<T> source, Action<T> observer)
        {
            return source.Subscribe(new EspObserver<T>(observer));
        }

        public static IDisposable Subscribe<T>(this IObservable<T> source, Action<T> observer, Action<Exception> onError)
        {
            return source.Subscribe(new EspObserver<T>(observer, onError));
        }

        public static IDisposable Subscribe<T>(this IObservable<T> source, Action<T> observer, Action<Exception> onError, Action onCompleted)
        {
            return source.Subscribe(new EspObserver<T>(observer, onError, onCompleted));
        }
    }
}
#endif