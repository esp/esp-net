using System;

namespace Esp.Net.Model
{
    public abstract class DisposableBase : IDisposable
    {
        private readonly DisposableCollection _disposables = new DisposableCollection();

        public void AddDisposable(IDisposable disposable)
        {
            _disposables.Add(disposable);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}