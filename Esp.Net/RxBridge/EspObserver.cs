using System;

namespace Esp.Net.RxBridge
{
    public class EspObserver<T> : IObserver<T>
    {
        private readonly Action<T> _observer;
        private readonly Action<Exception> _onError;
        private readonly Action _onCompleted;
        private bool _hasError;
        private bool _isComplted;

        public EspObserver(Action<T> observer)
            : this(observer, null, null)
        {
            _observer = observer;
        }

        public EspObserver(Action<T> observer, Action<Exception> onError)
            : this(observer, onError, null)
        {
        }

        public EspObserver(Action<T> observer, Action<Exception> onError, Action onCompleted)
        {
            _observer = observer;
            _onError = onError;
            _onCompleted = onCompleted;
        }

        public void OnNext(T value)
        {
            if (_isComplted || _hasError) return;
            _observer(value);
        }

        public void OnError(Exception error)
        {
            if (_isComplted || _hasError) return;
            _hasError = true;
            if (_onError != null) 
                _onError(error);
            else
                throw error;
        }

        public void OnCompleted()
        {
            if (_isComplted || _hasError) return;
            _isComplted = true;
            if (_onCompleted != null) _onCompleted();
        }
    }
}