using System;

namespace Esp.Net
{
    public partial class Router
    {
        private class RouterGuard
        {
            private readonly State _state;
            private readonly IThreadGuard _threadGuard;

            public RouterGuard(State state, IThreadGuard threadGuard)
            {
                _state = state;
                _threadGuard = threadGuard;
            }

            public void EnsureValid()
            {
                ThrowIfHalted();
                ThrowIfInvalidThread();
            }

            private void ThrowIfHalted()
            {
                if (_state.CurrentStatus == Status.Halted)
                {
                    throw _state.HaltingException;
                }
            }

            private void ThrowIfInvalidThread()
            {
                if (!_threadGuard.CheckAccess())
                {
                    throw new InvalidOperationException("Router called on invalid thread");
                }
            }
        }
    }
}