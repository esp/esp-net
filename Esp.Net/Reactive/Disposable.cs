using System;

namespace Esp.Net.Reactive
{
    public class Disposable : IDisposable
    {
        public static IDisposable Create(Action action)
        {
            return new Disposable(action);
        }

        private readonly Action _action;

        private Disposable(Action action)
        {
            _action = action;
        }

        public void Dispose()
        {
            _action();
        }
    }
}