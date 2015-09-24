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
using System.Linq;
using System.Reflection;
using Esp.Net.Meta;
using Esp.Net.Utils;
using Microsoft.CSharp.RuntimeBinder;

namespace Esp.Net
{
    public partial class Router
    {
        private interface IModelEntry
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

        private interface IModelEntry<out TModel> : IModelEntry
        {
            IModelObservable<TModel> GetModelObservable();
            IEventObservable<TModel, TEvent, IEventContext> GetEventObservable<TEvent>(ObservationStage observationStage = ObservationStage.Normal);
            IEventObservable<TModel, TBaseEvent, IEventContext> GetEventObservable<TBaseEvent>(Type subEventType, ObservationStage observationStage = ObservationStage.Normal);
        }

        private interface IModelChangedEventPublisher
        {
            void BroadcastEvent<TModel>(ModelChangedEvent<TModel> @event);
        }

        private class ModelEntry<TModel> : IModelEntry<TModel>
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
            private static readonly MethodInfo GetEventObservableMethodInfo = ReflectionHelper.GetGenericMethodByArgumentCount(typeof(ModelEntry<TModel>), "GetEventObservable", 1, 1);

            public ModelEntry(
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
                lock (_gate)
                {
                    if (!_eventSubjects.ContainsKey(typeof (TEvent))) return;
                }
                if (typeof (ModelChangedEvent<TModel>).IsAssignableFrom(typeof (TEvent)))
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
            /// Returns an event IEventObservable typed against TBaseEvent for the sub event of subEventType. This is useful when you combine mutiple events into a single stream 
            /// and care little for the high level type of the event.
            /// </summary>
            /// <typeparam name="TBaseEvent"></typeparam>
            /// <param name="subEventType"></param>
            /// <param name="observationStage"></param>
            /// <returns></returns>
            public IEventObservable<TModel, TBaseEvent, IEventContext> GetEventObservable<TBaseEvent>(Type subEventType, ObservationStage observationStage = ObservationStage.Normal)
            {
                Guard.Requires<ArgumentException>(typeof(TBaseEvent).IsAssignableFrom(subEventType), "Event type {0} must derive from {1}", subEventType, typeof(TBaseEvent));
                return EventObservable.Create<TModel, TBaseEvent, IEventContext>(o =>
                {
                    var getEventStreamMethod = GetEventObservableMethodInfo.MakeGenericMethod(subEventType);
                    try
                    {
                        dynamic observable = getEventStreamMethod.Invoke(this, new object[] { observationStage });
                        return (IDisposable)observable.Observe(o);
                    }
                    catch (RuntimeBinderException ex)
                    {
                        throw new Exception(string.Format("Error observing event of type [{0}]. Is this event scoped as private or internal? The Router uses the DLR to dispatch/observe events not reflection, it can't dispatch/observe internally scoped events without using a InternalsVisibleTo attribute.", subEventType.FullName), ex);
                    }
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
                    _state.ThrowIfHalted();
                    EventSubjects<TEvent> eventSubjects;
                    lock (_gate)
                    {
                        if (!_eventSubjects.ContainsKey(typeof (TEvent)))
                        {
                            eventSubjects = new EventSubjects<TEvent>(_eventObservationRegistrar);
                            _eventSubjects[typeof (TEvent)] = eventSubjects;
                        }
                        else
                        {
                            eventSubjects = (EventSubjects<TEvent>) _eventSubjects[typeof (TEvent)];
                        }
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
                        dynamic eventSubjects;
                        lock (_gate)
                        {
                            _eventSubjects.TryGetValue(typeof(TEvent), out eventSubjects);
                        }
                        if (eventSubjects != null)
                        {
                            var eventContext = new EventContext();
                            eventContext.CurrentStage = ObservationStage.Preview;
                            eventSubjects.PreviewSubject.OnNext(_model, @event, eventContext);
                            if (eventContext.IsCommitted) throw new InvalidOperationException(string.Format("Committing event [{0}] at the ObservationStage.Preview is invalid", @event.GetType().Name));
                            if (!eventContext.IsCanceled && !IsRemoved)
                            {
                                eventContext.CurrentStage = ObservationStage.Normal;
                                eventSubjects.NormalSubject.OnNext(_model, @event, eventContext);
                                if (eventContext.IsCanceled) throw new InvalidOperationException(string.Format("Cancelling event [{0}] at the ObservationStage.Normal is invalid", @event.GetType().Name));
                                if (eventContext.IsCommitted && !IsRemoved)
                                {
                                    eventContext.CurrentStage = ObservationStage.Committed;
                                    eventSubjects.CommittedSubject.OnNext(_model, @event, eventContext);
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
                    PreviewSubject = new EventSubject<TModel, TEvent, IEventContext>(observationRegistrar);
                    NormalSubject = new EventSubject<TModel, TEvent, IEventContext>(observationRegistrar);
                    CommittedSubject = new EventSubject<TModel, TEvent, IEventContext>(observationRegistrar);
                }

                public EventSubject<TModel, TEvent, IEventContext> PreviewSubject { get; private set; }
                public EventSubject<TModel, TEvent, IEventContext> NormalSubject { get; private set; }
                public EventSubject<TModel, TEvent, IEventContext> CommittedSubject { get; private set; }
            }
        }
    }
}