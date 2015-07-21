using System;
using System.Collections.Generic;
using System.Reflection;
using Esp.Net.Model;
using Esp.Net.Reactive;

namespace Esp.Net.Router
{
    internal interface IModelEntry
    {
        Guid Id { get; }
        bool HadEvents { get; }
        void Enqueue<TEvent>(TEvent @event);
        bool PurgeEventQueue();
        void RunPreProcessor();
        void RunPostProcessor();
        void DispatchModel();
    }

    internal interface IModelEntry<out TModel> : IModelEntry
    {
        IModelObservable<TModel> GetModelObservable();
        IEventObservable<TModel, TEvent, IEventContext> GetEventObservable<TEvent>(ObservationStage observationStage = ObservationStage.Normal);
        IEventObservable<TModel, TBaseEvent, IEventContext> GetEventObservable<TSubEventType, TBaseEvent>(ObservationStage observationStage = ObservationStage.Normal) where TSubEventType : TBaseEvent;
        IEventObservable<TModel, TBaseEvent, IEventContext> GetEventObservable<TBaseEvent>(Type eventType, ObservationStage observationStage = ObservationStage.Normal);
    }

    internal class ModelEntry<TModel> : IModelEntry<TModel>
    {
        private readonly TModel _model;
        private readonly IPreEventProcessor<TModel> _preEventProcessor;
        private readonly IPostEventProcessor<TModel> _postEventProcessor;
        private readonly RouterGuard _routerGuard;
        private readonly Queue<dynamic> _eventDispatchQueue = new Queue<dynamic>();
        private readonly Dictionary<Type, dynamic> _eventSubjects = new Dictionary<Type, dynamic>();
        private readonly ModelSubject<TModel> _modelUpdateSubject = new ModelSubject<TModel>();
        private static readonly MethodInfo GetEventObservableMethodInfo = ReflectionHelper.GetGenericMethodByArgumentCount(typeof(ModelEntry<TModel>), "GetEventObservable", 1, 1);

        public ModelEntry(Guid id, TModel model, RouterGuard routerGuard)
            : this(id, model, null, null, routerGuard)
        {
        }

        public ModelEntry(Guid id, TModel model, IPreEventProcessor<TModel> preEventProcessor, RouterGuard routerGuard)
            : this(id, model, preEventProcessor, null, routerGuard)
        {
        }

        public ModelEntry(Guid id, TModel model, IPostEventProcessor<TModel> postEventProcessor, RouterGuard routerGuard)
            : this(id, model, null, postEventProcessor, routerGuard)
        {
        }

        public ModelEntry(Guid id, TModel model, IPreEventProcessor<TModel> preEventProcessor, IPostEventProcessor<TModel> postEventProcessor, RouterGuard routerGuard)
        {
            Id = id;
            _model = model;
            _preEventProcessor = preEventProcessor;
            _postEventProcessor = postEventProcessor;
            _routerGuard = routerGuard;
        }

        public Guid Id { get; private set; }

        public bool HadEvents { get { return _eventDispatchQueue.Count > 0; } }

        public void Enqueue<TEvent>(TEvent @event)
        {
            _eventDispatchQueue.Enqueue(ProcessEvent(@event));
        }

        public bool PurgeEventQueue()
        {
            bool hasEvents = _eventDispatchQueue.Count > 0;
            bool eventWasDispatched = false;
            while (hasEvents)
            {
                while (hasEvents)
                {
                    var dispatchAction = _eventDispatchQueue.Dequeue();
                    var wasDispatched1 = dispatchAction();
                    if (!eventWasDispatched && wasDispatched1) eventWasDispatched = true;
                    hasEvents = _eventDispatchQueue.Count > 0;
                }
                if (_postEventProcessor != null) _postEventProcessor.Process(_model);
                hasEvents = _eventDispatchQueue.Count > 0;
            }
            return eventWasDispatched;
        }

        public void RunPreProcessor()
        {
            if (_preEventProcessor != null) _preEventProcessor.Process(_model);
        }

        public void RunPostProcessor()
        {
            if (_preEventProcessor != null) _preEventProcessor.Process(_model);
        }

        public void DispatchModel()
        {
            var cloneable = _model as ICloneable<TModel>;
            TModel modelToDispatch = cloneable == null
                ? _model
                : cloneable.Clone();
            _modelUpdateSubject.OnNext(modelToDispatch);
        }

        public IModelObservable<TModel> GetModelObservable()
        {
            return ModelObservable.Create<TModel>(o =>
            {
                _routerGuard.EnsureValid();
                return _modelUpdateSubject.Observe(o);
            });
        }

        /// <summary>
        /// Returns an event IEventObservable typed against TBaseEvent for the sub event of eventType. This is useful when you combine mutiple events into a single stream 
        /// and care little for the high level type of the event.
        /// </summary>
        /// <typeparam name="TSubEventType"></typeparam>
        /// <typeparam name="TBaseEvent"></typeparam>
        /// <param name="observationStage"></param>
        /// <returns></returns>
        public IEventObservable<TModel, TBaseEvent, IEventContext> GetEventObservable<TSubEventType, TBaseEvent>(ObservationStage observationStage = ObservationStage.Normal)
            where TSubEventType : TBaseEvent
        {
            return GetEventObservable<TBaseEvent>(typeof(TSubEventType));
        }

        /// <summary>
        /// Returns an event IEventObservable typed against TBaseEvent for the sub event of eventType. This is useful when you combine mutiple events into a single stream 
        /// and care little for the high level type of the event.
        /// </summary>
        /// <typeparam name="TBaseEvent"></typeparam>
        /// <param name="eventType"></param>
        /// <param name="observationStage"></param>
        /// <returns></returns>
        public IEventObservable<TModel, TBaseEvent, IEventContext> GetEventObservable<TBaseEvent>(Type eventType, ObservationStage observationStage = ObservationStage.Normal)
        {
            Guard.Requires<InvalidOperationException>(typeof(TBaseEvent).IsAssignableFrom(eventType), "Event type {0} must derive from {1}", eventType, typeof(TBaseEvent));
            return EventObservable.Create<TModel, TBaseEvent, IEventContext>(o =>
            {
                _routerGuard.EnsureValid();
                var getEventStreamMethod = GetEventObservableMethodInfo.MakeGenericMethod(eventType);
                dynamic observable = getEventStreamMethod.Invoke(this, new object[] { observationStage });
                return (IDisposable)observable.Observe(o);
            });
        }

        /// <summary>
        /// Returns an IEventObservable that will yield events of type TEvent when observed.
        /// </summary>
        /// <typeparam name="TEvent">Type type of event to observe</typeparam>
        /// <param name="observationStage">The stage in the event processing workflow you wish to observe at</param>
        /// <returns></returns>
        public IEventObservable<TModel, TEvent, IEventContext> GetEventObservable<TEvent>(ObservationStage observationStage = ObservationStage.Normal)
        {
            return EventObservable.Create<TModel, TEvent, IEventContext>(o =>
            {
                _routerGuard.EnsureValid();
                EventSubjects<TEvent> eventSubjects;
                if (!_eventSubjects.ContainsKey(typeof(TEvent)))
                {
                    eventSubjects = new EventSubjects<TEvent>();
                    _eventSubjects[typeof(TEvent)] = eventSubjects;
                }
                else
                {
                    eventSubjects = (EventSubjects<TEvent>)_eventSubjects[typeof(TEvent)];
                }
                EventSubject<TModel, TEvent, IEventContext> subject;
                switch (observationStage)
                {
                    case ObservationStage.Preview:
                        subject = eventSubjects.PreviewSubject;
                        break;
                    case ObservationStage.Normal:
                        subject = eventSubjects.NormalSubject;
                        break;
                    case ObservationStage.Committed:
                        subject = eventSubjects.CommittedSubject;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("observationStage " + observationStage + " not supported", observationStage, null);
                }
                return subject.Observe(o);
            });
        }

        private Func<bool> ProcessEvent<TEvent>(TEvent @event)
        {
            return () =>
            {
                dynamic eventSubjects;
                if (_eventSubjects.TryGetValue(typeof(TEvent), out eventSubjects))
                {
                    var eventContext = new EventContext();
                    eventSubjects.PreviewSubject.OnNext(_model, @event, eventContext);
                    if (!eventContext.IsCanceled)
                    {
                        eventSubjects.NormalSubject.OnNext(_model, @event, eventContext);
                        if (eventContext.IsCommitted)
                        {
                            eventSubjects.CommittedSubject.OnNext(_model, @event, eventContext);
                        }
                    }
                    return true;
                }
                return false;
            };
        }



        private class EventSubjects<TEvent>
        {
            public EventSubjects()
            {
                PreviewSubject = new EventSubject<TModel, TEvent, IEventContext>();
                NormalSubject = new EventSubject<TModel, TEvent, IEventContext>();
                CommittedSubject = new EventSubject<TModel, TEvent, IEventContext>();
            }

            public EventSubject<TModel, TEvent, IEventContext> PreviewSubject { get; private set; }
            public EventSubject<TModel, TEvent, IEventContext> NormalSubject { get; private set; }
            public EventSubject<TModel, TEvent, IEventContext> CommittedSubject { get; private set; }
        }
    }
}