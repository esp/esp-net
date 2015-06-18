using System;

namespace Esp.Net
{
    public interface IEventContext<out TModel, out TEvent>
    {
        TModel Model { get; }
        TEvent Event { get; }
        bool IsCanceled { get; }
        void Cancel();
        void Commit();
    }

    public class EventContext<TModel, TEvent> : IEventContext<TModel, TEvent>
    {
        private bool _isCanceled;
        private bool _isCommitted;

        public EventContext(TModel model, TEvent @event)
        {
            Model = model;
            Event = @event;
        }

        public TModel Model { get; private set; }

        public TEvent Event { get; private set; }

        public bool IsCanceled 
        {
            get { return _isCanceled; }
        }

        public bool IsCommitted
        {
            get { return _isCommitted; }
        }

        public void Cancel()
        {
            if(_isCanceled) throw new Exception("Already canceled");
            _isCanceled = true;
        }
        
        public void Commit()
        {
            if (_isCommitted) throw new Exception("Already committed");
            _isCommitted = true;
        }
    }
}