using System;
using System.Collections.Generic;

namespace Esp.Net.Reactive
{
    public class DisposableCollection : IDisposable
    {
        private readonly List<IDisposable> _disposables;

        public DisposableCollection(params IDisposable[] disposables)
        {
            _disposables = new List<IDisposable>(disposables);
        }

        public bool IsDisposed { get; private set; }

        public void Add(IDisposable disposable)
        {
            if (IsDisposed)
            {
                disposable.Dispose();
                return;
            }
            _disposables.Add(disposable);
        }

        public void Dispose()
        {
            if(IsDisposed) return;
            IsDisposed = true;
            foreach (IDisposable disposable in _disposables)
            {
                disposable.Dispose();
            }
        }
    }
}