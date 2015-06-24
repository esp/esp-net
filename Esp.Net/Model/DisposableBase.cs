using System;

namespace Esp.Net.Model
{
    public abstract class DisposableBase : IDisposable
    {
        private readonly DisposableCollection _disposables = new DisposableCollection();
        private bool _isDisposed;

        public void AddDisposable(IDisposable disposable)
        {
            if (_isDisposed)
            {
                disposable.Dispose();
                return;
            }
            _disposables.Add(disposable);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;
            _disposables.Dispose();
        }
    }
}