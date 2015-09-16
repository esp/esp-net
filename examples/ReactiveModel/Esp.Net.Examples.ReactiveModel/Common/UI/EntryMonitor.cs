using System;
using System.Reactive.Disposables;

namespace Esp.Net.Examples.ReactiveModel.Common.UI
{
    public class EntryMonitor
    {
        private int _enterCount;

        public bool IsBusy
        {
            get { return _enterCount > 0; }
        }

        public IDisposable Enter()
        {
            _enterCount++;
            return Disposable.Create(() => _enterCount-- );
        }
    }
}