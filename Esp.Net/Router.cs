#region copyright
// Copyright 2015 Keith Woods
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion
using System;
using System.Collections.Generic;
using System.Reflection;
using Esp.Net.Reactive;

namespace Esp.Net
{
    public interface IRouter<out TModel>
    {
        void PublishEvent<TEvent>(TEvent @event);
        IEventObservable<TModel> GetModelObservable();
        IEventObservable<IEventContext<TModel, TEvent>> GetEventObservable<TEvent>(ObservationStage observationStage = ObservationStage.Normal);
        IEventObservable<IEventContext<TModel, TBaseEvent>> GetEventObservable<TBaseEvent>(Type eventType, ObservationStage observationStage = ObservationStage.Normal);
    }

    public class Router<TModel> : IRouter<TModel>
    {
        private readonly TModel _model;
        private readonly IRouterScheduler _scheduler;
        private readonly IPreEventProcessor<TModel> _preEventProcessor;
        private readonly IPostEventProcessor<TModel> _postEventProcessor;
        private readonly Queue<dynamic> _eventDispatchQueue = new Queue<dynamic>();
        private readonly Dictionary<Type, dynamic> _eventSubjects = new Dictionary<Type, dynamic>();
        private readonly State _state = new State();
        private readonly EventSubject<TModel> _modelUpdateSubject = new EventSubject<TModel>();
        private static readonly MethodInfo GetEventStreamMethodInfo = typeof(Router<TModel>).GetMethod("GetEventObservable", new[] { typeof(ObservationStage) });

        public Router(TModel model, IRouterScheduler scheduler)
            : this(model, scheduler, null, null)
        {
        }

        public Router(TModel model, IRouterScheduler scheduler, IPreEventProcessor<TModel> preEventProcessor)
            : this(model, scheduler, preEventProcessor, null)
        {
        }

        public Router(TModel model, IRouterScheduler scheduler, IPostEventProcessor<TModel> postEventProcessor)
            : this(model, scheduler, null, postEventProcessor)
        {
        }

        public Router(TModel model, IRouterScheduler scheduler, IPreEventProcessor<TModel> preEventProcessor, IPostEventProcessor<TModel> postEventProcessor)
        {
            _model = model;
            _scheduler = scheduler;
            _preEventProcessor = preEventProcessor;
            _postEventProcessor = postEventProcessor;
        }

        public void PublishEvent<TEvent>(TEvent @event)
        {
            ThrowIfHalted();
            ThrowIfInvalidThread();
            _eventDispatchQueue.Enqueue(ProcessEvent(@event));
            PurgeEventQueue();
        }

        private void PurgeEventQueue()
        {
            if (_state.CurrentStatus == Status.Idle)
            {
                try
                {
                    bool hasEvents = _eventDispatchQueue.Count > 0;

                    while (hasEvents)
                    {
                        bool wasDispatched = false;
                        while (hasEvents)
                        {
                            _state.MoveToPreProcessing();
                            if (_preEventProcessor != null) _preEventProcessor.Process(_model);
                            _state.MoveToEventDispatch();
                            while (hasEvents)
                            {
                                var dispatchAction = _eventDispatchQueue.Dequeue();
                                var wasDispatched1 = dispatchAction();
                                if (!wasDispatched && wasDispatched1) wasDispatched = true;
                                hasEvents = _eventDispatchQueue.Count > 0;
                            }
                            _state.MoveToPostProcessing();
                            if (_postEventProcessor != null) _postEventProcessor.Process(_model);
                            hasEvents = _eventDispatchQueue.Count > 0;
                        }
                        _state.MoveToDispatchModelUpdates();
                        if (wasDispatched)
                        {
                            var cloneable = _model as ICloneable<TModel>;
                            TModel modelToDispatch = cloneable == null 
                                ? _model 
                                : cloneable.Clone();
                            _modelUpdateSubject.OnNext(modelToDispatch);
                        }
                        hasEvents = _eventDispatchQueue.Count > 0;
                    }
                    _state.MoveToIdle();
                }
                catch (Exception ex)
                {
                    _state.MoveToHalted(ex);
                    throw;
                }
            }
        }

        public IEventObservable<TModel> GetModelObservable()
        {
            ThrowIfHalted();
            return EventObservable.Create<TModel>(o =>
            {
                ThrowIfHalted();
                ThrowIfInvalidThread();
                return _modelUpdateSubject.Observe(o);
            });
        }

        public IEventObservable<IEventContext<TModel, TBaseEvent>> GetEventObservable<TBaseEvent>(Type eventType, ObservationStage observationStage = ObservationStage.Normal)
        {
            ThrowIfHalted();
            return EventObservable.Create<IEventContext<TModel, TBaseEvent>>(o =>
            {
                ThrowIfHalted();
                ThrowIfInvalidThread();
                var getEventStreamMethod = GetEventStreamMethodInfo.MakeGenericMethod(eventType);
                dynamic observable = getEventStreamMethod.Invoke(this, new object[] { observationStage });
                return (IDisposable)observable.Observe(o);
            });
        }

        public IEventObservable<IEventContext<TModel, TEvent>> GetEventObservable<TEvent>(ObservationStage observationStage = ObservationStage.Normal)
        {
            ThrowIfHalted();
            return EventObservable.Create<IEventContext<TModel, TEvent>>(o =>
            {
                ThrowIfHalted();
                ThrowIfInvalidThread();
                EventSubjects<IEventContext<TModel, TEvent>> eventSubjects;
                if (!_eventSubjects.ContainsKey(typeof (TEvent)))
                {
                    eventSubjects = new EventSubjects<IEventContext<TModel, TEvent>>();
                    _eventSubjects[typeof (TEvent)] = eventSubjects;
                }
                else
                {
                    eventSubjects = (EventSubjects<IEventContext<TModel, TEvent>>)_eventSubjects[typeof(TEvent)];
                }
                EventSubject<IEventContext<TModel, TEvent>> subject;
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
                if (_eventSubjects.TryGetValue(typeof (TEvent), out eventSubjects))
                {
                    var eventContext = new EventContext<TModel, TEvent>(_model, @event);
                    eventSubjects.PreviewSubject.OnNext(eventContext);
                    if (!eventContext.IsCanceled)
                    {
                        eventSubjects.NormalSubject.OnNext(eventContext);
                        if (eventContext.IsCommitted)
                        {
                            eventSubjects.CommittedSubject.OnNext(eventContext);
                        }
                    }
                    return true;
                }
                return false;
            };
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
            if(!_scheduler.Checkaccess())
            {
                throw new InvalidOperationException("Router called on invalid thread");
            }
        }

        private class EventSubjects<TEventEnvelope>
        {
            public EventSubjects()
            {
                PreviewSubject = new EventSubject<TEventEnvelope>();
                NormalSubject = new EventSubject<TEventEnvelope>();
                CommittedSubject = new EventSubject<TEventEnvelope>();
            }

            public EventSubject<TEventEnvelope> PreviewSubject { get; private set; }
            public EventSubject<TEventEnvelope> NormalSubject { get; private set; }
            public EventSubject<TEventEnvelope> CommittedSubject { get; private set; }
        }

        private class State
        {
            public State()
            {
                CurrentStatus = Status.Idle;
            }

            public Exception HaltingException { get; private set; }
            
            public Status CurrentStatus { get; private set; }

            public void MoveToPreProcessing()
            {
                CurrentStatus = Status.PreEventProcessing;
            }

            public void MoveToEventDispatch()
            {
                CurrentStatus = Status.EventProcessorDispatch;
            }

            public void MoveToPostProcessing()
            {
                CurrentStatus = Status.PostProcessing;
            }

            public void MoveToDispatchModelUpdates()
            {
                CurrentStatus = Status.DispatchModelUpdates;
            }

            public void MoveToHalted(Exception exception)
            {
                HaltingException = exception;
                CurrentStatus = Status.Halted;
            }

            public void MoveToIdle()
            {
                CurrentStatus = Status.Idle;
            }
        }

        private enum Status
        {
            Idle,
            PreEventProcessing,
            EventProcessorDispatch,
            PostProcessing,
            DispatchModelUpdates,
            Halted,
        }
    }
}
