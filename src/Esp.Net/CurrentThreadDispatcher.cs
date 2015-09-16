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
            if (!CheckAccess())
                throw new InvalidOperationException(
                    string.Format(
                        "The dispatcher [{0}] can not marshal a dispatch call onto the thread with id {1}. If you want to access the router from any thread uses a dispatcher that supports multi threaded applications. Alternatively ensure that you're always on the same thread that created the Router (which his thread with id {1})",
                        GetType().FullName,
                        _threadId
                    )
                );

            if (!_isDisposed)
            {
                action();
            }
        }
    }
}