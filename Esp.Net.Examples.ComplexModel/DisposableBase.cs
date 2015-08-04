using System;
using System.Collections.Generic;

namespace Esp.Net.Examples.ComplexModel
{
    internal abstract class DisposableBase : IDisposable
    {
        private readonly List<IDisposable> _disposables;

        protected DisposableBase(params IDisposable[] disposables)
        {
            _disposables = new List<IDisposable>(disposables);
        }

        public bool IsDisposed { get; private set; }

        public void AddDisposable(IDisposable disposable)
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