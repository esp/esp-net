using System;

namespace Esp.Net.Disposables
{
    internal class EspSerialDisposable : IDisposable
    {
        private bool _isDisposed;

        private IDisposable _disposable;

        public IDisposable Disposable
        {
            get { return _disposable; }
            set
            {
                using (_disposable) { }
                if (_isDisposed) 
                    using (value) { }
                else
                    _disposable = value;
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            using (_disposable) { }
        }
    }
}