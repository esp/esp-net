using System;

namespace Esp.Net.Disposables
{
    internal abstract class DisposableBase : IDisposable
    {
        private readonly CollectionDisposable _disposables = new CollectionDisposable();

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