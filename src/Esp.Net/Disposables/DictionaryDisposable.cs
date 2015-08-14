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
using System.Collections.Generic;

namespace Esp.Net.Disposables
{
    public class DictionaryDisposable<TKey> : IDisposable
    {
        private readonly Dictionary<TKey, IDisposable> _disposables = new Dictionary<TKey, IDisposable>();

        public bool IsDisposed { get; private set; }
        
        public void Add(TKey key, IDisposable disposable)
        {
            if (IsDisposed)
            {
                disposable.Dispose();
                return;
            }
            _disposables.Add(key, disposable);
        }

        public bool Remove(TKey key)
        {
            return _disposables.Remove(key);
        }

        public void Dispose()
        {
            if(IsDisposed) return;
            IsDisposed = true;

            foreach (IDisposable disposable in _disposables.Values)
            {
                disposable.Dispose();
            }
        }
    }
}