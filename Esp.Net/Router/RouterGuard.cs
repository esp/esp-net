using System;

namespace Esp.Net.Router
{
    internal class RouterGuard
    {
        private readonly State _state;
        private readonly IThreadGuard _threadGuard;

        public RouterGuard(State state, IThreadGuard threadGuard)
        {
            _state = state;
            _threadGuard = threadGuard;
        }

        internal void EnsureValid()
        {
            ThrowIfHalted();
            ThrowIfInvalidThread();
        }

        internal void ThrowIfHalted()
        {
            if (_state.CurrentStatus == Status.Halted)
            {
                throw _state.HaltingException;
            }
        }

        internal void ThrowIfInvalidThread()
        {
            if (!_threadGuard.CheckAccess())
            {
                throw new InvalidOperationException("Router called on invalid thread");
            }
        } 
    }
}