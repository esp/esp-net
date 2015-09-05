using System;
using System.Threading;

namespace Esp.Net
{
    public class CurrentThreadDispatcher : IRouterDispatcher
    {
        private readonly int _threadId;
        private bool _isDisposed;

        public CurrentThreadDispatcher()
        {
            _threadId = Thread.CurrentThread.ManagedThreadId;
        }

        public void Dispose()
        {
            EnsureAccess();
            if (!_isDisposed) _isDisposed = true;
        }

        public bool CheckAccess()
        {
            return Thread.CurrentThread.ManagedThreadId == _threadId;
        }

        public void EnsureAccess()
        {
            if (!CheckAccess())
            {
                throw new InvalidOperationException("Router accessed on invalid thread");
            }
        }

        public void Dispatch(Action action)
        {
            EnsureAccess();
            if (!_isDisposed)
            {
                action();
            }
        }
    }
}