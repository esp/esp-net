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
    public class EspDisposable : IDisposable
    {
        public static IDisposable Empty { get; private set; }

        static EspDisposable()
        {
            Empty = new EspDisposable(() => { /* Noop*/ });
        }
        public bool IsDisposed { get; private set; }

        public static IDisposable Create(Action action)
        {
            return new EspDisposable(action);
        }

        private readonly Action _action;

        private EspDisposable(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action", "Action must not be null.");
            }
            _action = action;
        }

        public void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;
            _action();
        }
    }
}