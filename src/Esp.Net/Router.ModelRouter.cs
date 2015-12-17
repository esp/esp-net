#region copyright
// Copyright 2015 Dev Shop Limited
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
using System.Linq;
using System.Reflection;
using Esp.Net.Meta;
using Esp.Net.Utils;
using Microsoft.CSharp.RuntimeBinder;

namespace Esp.Net
{
    public partial class Router
    {
        private interface IModelRouter
        {
            object Id { get; }
            bool HadEvents { get; }
            bool IsRemoved { get; }
            void TryEnqueue<TEvent>(TEvent @event);
            void ExecuteEvent<TEvent>(TEvent @event);
            void RunAction(Action action);
            void RunAction<TModel>(Action<TModel> action);
            void PurgeEventQueue();
            void RunPreProcessor();
            void RunPostProcessor();
            void DispatchModel();
            void OnRemoved();
            void BroadcastModelChangedEvent();
        }

        private interface IModelRouter<out TModel> : IModelRouter
        {
            IModelObservable<TModel> GetModelObservable();
            IEventObservable<TEvent, IEventContext, TModel> GetEventObservable<TEvent>(ObservationStage observationStage = ObservationStage.Normal);
        }

        private interface IModelChangedEventPublisher
        {
            void BroadcastEvent<TModel>(ModelChangedEvent<TModel> @event);
        }

        private class ModelRouter<TModel> : IModelRouter<TModel>
        {
            private readonly TModel _model;
            private readonly IPreEventProcessor<TModel> _preEventProcessor;
            private readonly IPreEventProcessor _modelAsPreEventProcessor;
            private readonly IPostEventProcessor<TModel> _postEventProcessor;
            private readonly IPostEventProcessor _modelAsPostEventProcessor;
            private readonly State _state;
            private readonly IEventObservationRegistrar _eventObservationRegistrar;
            private readonly IModelChangedEventPublisher _modelChangedEventPublisher;
            private readonly Queue<Action> _eventDispatchQueue = new Queue<Action>();
            private readonly Dictionary<Type, dynamic> _eventSubjects = new Dictionary<Type, dynamic>();
            private readonly ModelSubject<TModel> _modelUpdateSubject = new ModelSubject<TModel>();
            private readonly object _gate = new object();

            public ModelRouter(
                object id, 
                TModel model, 
                IPreEventProcessor<TModel> preEventProcessor, 
                IPostEventProcessor<TModel> postEventProcessor, 
                State state,
                IEventObservationRegistrar eventObservationRegistrar,
                IModelChangedEventPublisher modelChangedEventPublisher
            )
            {
                Id = id;
                _model = model;
                _preEventProcessor = preEventProcessor;
                _modelAsPreEventProcessor = model as IPreEventProcessor;
                _postEventProcessor = postEventProcessor;
                _modelAsPostEventProcessor = model as IPostEventProcessor;
                _state = state;
                _eventObservationRegistrar = eventObservationRegistrar;
                _modelChangedEventPublisher = modelChangedEventPublisher;
            }

            public object Id { get; private set; }

            public bool HadEvents { get { return _eventDispatchQueue.Count > 0; } }
            
            public bool IsRemoved { get; private set; }

            public void TryEnqueue<TEvent>(TEvent @event)
            {
                var eventType = typeof (TEvent);
                lock (_gate)
                {
                    bool foundObserver = _eventSubjects.ContainsKey(eventType);
                    if(!foundObserver)
                    {
                        var baseEventType = eventType.BaseType;
                        while (baseEventType != null)
                        {
                            foundObserver = _eventSubjects.ContainsKey(baseEventType);
                            if (foundObserver) break;
                            baseEventType = baseEventType.BaseType;
                        }
                    }
                    if (!foundObserver) return;
                }
                if (typeof (ModelChangedEvent<TModel>).IsAssignableFrom(eventType))
                {
                    var message = string.Format("The event stream observing event ModelChangedEvent<{0}> against model of type [{0}] is unsupported. Observing a ModelChangedEvent<T> where T is the same as the target models type is not supported.", typeof(TModel).Name);
                    throw new NotSupportedException(message);
                }
                _eventDispatchQueue.Enqueue(CreateEventDispatchAction(@event));
            }

            public void ExecuteEvent<TEvent>(TEvent @event)
            {
                Action dispatchAction = CreateEventDispatchAction(@event);
                dispatchAction();
            }

            public void RunAction(Action action)
            {
                _eventDispatchQueue.Enqueue(action);
            }

            public void RunAction<TModel1>(Action<TModel1> action)
            {
                dynamic dAction = action;
                _eventDispatchQueue.Enqueue(() => dAction(_model));
            }

            public void PurgeEventQueue()
            {
                bool hasEvents = _eventDispatchQueue.Count > 0;
                while (hasEvents)
                {
                    dynamic dispatchAction = _eventDispatchQueue.Dequeue();
                    dispatchAction();
                    hasEvents = _eventDispatchQueue.Count > 0;
                }
            }

            public void RunPreProcessor()
            {
                if (_preEventProcessor != null) _preEventProcessor.Process(_model);
                if (_modelAsPreEventProcessor != null) _modelAsPreEventProcessor.Process();
            }

            public void RunPostProcessor()
            {
                if (_postEventProcessor != null) _postEventProcessor.Process(_model);
                if (_modelAsPostEventProcessor != null) _modelAsPostEventProcessor.Process();
            }

            public void DispatchModel()
            {
                var cloneable = _model as ICloneable<TModel>;
                TModel modelToDispatch = cloneable == null
                    ? _model
                    : cloneable.Clone();
                _modelUpdateSubject.OnNext(modelToDispatch);
            }

            public void OnRemoved()
            {
                IsRemoved = true;
                dynamic[] eventSubjectss;
                lock (_gate)
                {
                    eventSubjectss = _eventSubjects.Values.ToArray();
                }
                foreach (dynamic eventSubjects in eventSubjectss)
                {
                    eventSubjects.PreviewSubject.OnCompleted();
                    eventSubjects.NormalSubject.OnCompleted();
                    eventSubjects.CommittedSubject.OnCompleted();
                }
                _modelUpdateSubject.OnCompleted();
            }

            public void BroadcastModelChangedEvent()
            {
                var modelChangedEvent = new ModelChangedEvent<TModel>(Id, _model);
                _modelChangedEventPublisher.BroadcastEvent(modelChangedEvent);
            }

            public IModelObservable<TModel> GetModelObservable()
            {
                return ModelObservable.Create<TModel>(o =>
                {
                    _state.ThrowIfHalted();
                    return _modelUpdateSubject.Observe(o);
                });
            }

            /// <summary>
            /// Returns an IEventObservable that will yield events of type TEvent when observed.
            /// </summary>
            /// <typeparam name="TEvent">Type type of event to observe</typeparam>
            /// <param name="observationStage">The stage in the event processing workflow you wish to observe at</param>
            /// <returns></returns>
            public IEventObservable<TEvent, IEventContext, TModel> GetEventObservable<TEvent>(ObservationStage observationStage = ObservationStage.Normal)
            {
                return EventObservable.Create<TEvent, IEventContext, TModel>(o =>
                {
                    _state.ThrowIfHalted();
                    EventSubjects<TEvent> eventSubjects;
                    lock (_gate)
                    {
                        var eventType = typeof(TEvent);
                        if (!_eventSubjects.ContainsKey(eventType))
                        {
                            eventSubjects = new EventSubjects<TEvent>(_eventObservationRegistrar);
                            _eventSubjects[eventType] = eventSubjects;
                        }
                        else
                        {
                            eventSubjects = (EventSubjects<TEvent>) _eventSubjects[eventType];
                        }
                    }
                    EventSubject<TEvent, IEventContext, TModel> subject;
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
                            throw new ArgumentOutOfRangeException(string.Format("observationStage {0} not supported", observationStage));
                    }
                    return subject.Observe(o);
                });
            }

            private Action CreateEventDispatchAction<TEvent>(TEvent @event)
            {
                return () =>
                {
                    try
                    {
                        List<dynamic> eventSubjectss = new List<dynamic>();
                        lock (_gate)
                        {
                            var mostDerivedEventType = @event.GetType();
                            while (mostDerivedEventType != null)
                            {
                                dynamic eventSubjects;
                                if(_eventSubjects.TryGetValue(mostDerivedEventType, out eventSubjects))
                                    eventSubjectss.Add(eventSubjects);
                                mostDerivedEventType = mostDerivedEventType.BaseType;
                            }
                        }
                        if (eventSubjectss.Count > 0)
                        {
                            var eventContext = new EventContext();
                            eventContext.CurrentStage = ObservationStage.Preview;
                            foreach (dynamic subjects in eventSubjectss)
                                subjects.PreviewSubject.OnNext(@event, eventContext, _model);
                            if (eventContext.IsCommitted) throw new InvalidOperationException(string.Format("Committing event [{0}] at the ObservationStage.Preview is invalid", @event.GetType().Name));
                            if (!eventContext.IsCanceled && !IsRemoved)
                            {
                                eventContext.CurrentStage = ObservationStage.Normal;
                                foreach (dynamic subjects in eventSubjectss)
                                    subjects.NormalSubject.OnNext(@event, eventContext, _model);
                                if (eventContext.IsCanceled) throw new InvalidOperationException(string.Format("Cancelling event [{0}] at the ObservationStage.Normal is invalid", @event.GetType().Name));
                                if (eventContext.IsCommitted && !IsRemoved)
                                {
                                    eventContext.CurrentStage = ObservationStage.Committed;
                                    foreach (dynamic subjects in eventSubjectss)
                                        subjects.CommittedSubject.OnNext(@event, eventContext, _model);
                                    if (eventContext.IsCanceled) throw new InvalidOperationException(string.Format("Cancelling event [{0}] at the ObservationStage.Committed is invalid", @event.GetType().Name));
                                }
                            }
                        }
                    }
                    catch (RuntimeBinderException ex)
                    {
                        throw new Exception(string.Format("Error dispatching event of type [{0}]. Is this event scoped as private or internal? The Router uses the DLR to dispatch/observe events not reflection, it can't dispatch/observe internally scoped events without using a InternalsVisibleTo attribute.", @event.GetType().FullName), ex);
                    }
                };
            }

            private class EventSubjects<TEvent>
            {
                public EventSubjects(IEventObservationRegistrar observationRegistrar)
                {
                    PreviewSubject = new EventSubject<TEvent, IEventContext, TModel>(observationRegistrar);
                    NormalSubject = new EventSubject<TEvent, IEventContext, TModel>(observationRegistrar);
                    CommittedSubject = new EventSubject<TEvent, IEventContext, TModel>(observationRegistrar);
                }

                public EventSubject<TEvent, IEventContext, TModel> PreviewSubject { get; private set; }
                public EventSubject<TEvent, IEventContext, TModel> NormalSubject { get; private set; }
                public EventSubject<TEvent, IEventContext, TModel> CommittedSubject { get; private set; }
            }
        }
    }
}