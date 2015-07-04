using System;
using System.Collections.Generic;

namespace Esp.Net.Model
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