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
using System.Linq.Expressions;
using System.Reflection;
using Esp.Net.Utils;

namespace Esp.Net
{
    public static partial class RouterExt
    {
        public static IDisposable ObserveEventsOn<TModel>(this IRouter<TModel> router, object eventProcessor)
        {
            var eventObservationRegistrar = new EventObservationRegistrar<TModel>(router, eventProcessor);
            eventObservationRegistrar.ObserveEvents();
            return eventObservationRegistrar;
        }

        public class EventObservationRegistrar<TModel> : DisposableBase
        {
            //private static readonly MethodInfo ObserveBaseEventsMethodInfo = ReflectionHelper.GetGenericMethodByArgumentCount(typeof(EventObservationRegistrar<TModel, TEventProcessor>), "ObserveBaseEvents", 1, 2, BindingFlags.Instance | BindingFlags.NonPublic);

            private static readonly MethodInfo GetEventObservableMethodInfo = ReflectionHelper.GetGenericMethodByArgumentCount(typeof(IRouter<TModel>), "GetEventObservable", 1, 1);

//            private static readonly MethodInfo ObservableMergeMethodInfo = ReflectionHelper.GetGenericMethodByArgumentCount(
//                typeof(EventObservable),
//                "Merge",
//                3,
//                1,
//                BindingFlags.Public | BindingFlags.Static,
//                p => p.Length == 0 && typeof(IEnumerable<>).IsAssignableFrom(p[0].ParameterType)
//            );

            private readonly object _eventProcessor;
            private readonly IRouter<TModel> _router;

            public EventObservationRegistrar(IRouter<TModel> router, object eventProcessor)
            {
                _eventProcessor = eventProcessor;
                _router = router;
            }

            public void ObserveEvents()
            {
                var methodsWithAttributes =
                    from methodInfo in _eventProcessor.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    let observeEventAttributes = methodInfo.GetCustomAttributes<ObserveEventAttribute>(true).ToArray()
                    where observeEventAttributes.Length > 0
                    select new { methodInfo, observeEventAttributes };
                foreach (var methodWithAttributes in methodsWithAttributes)
                {
                    if (methodWithAttributes.observeEventAttributes.Length == 1)
                    {
                        var eventObservable = GetEventObservable(methodWithAttributes.methodInfo, methodWithAttributes.observeEventAttributes[0]);
                        ObserveEvent(methodWithAttributes.methodInfo,
                             methodWithAttributes.observeEventAttributes[0].EventType,
                             eventObservable
                        );
                    }
//                    else
//                    {
//                        // which event is the type to use for the merged observable stream?
//                        // 1) the event that is in the method && common to all events on the attributes
//                        // 2) if no event on method, then the lowest common event base.
//
//                        var eventsObs = new List<object>();
//                        foreach (ObserveEventAttribute observeEventAttribute in methodWithAttributes.observeEventAttributes)
//                        {
//                            eventsObs.Add(GetEventObservable(methodWithAttributes.methodInfo, observeEventAttribute));
//                        }
//
//                        var observeBaseEvents = ObservableMergeMethodInfo.MakeGenericMethod(new Type[] { baseEventType });
//
//
//                        dynamic merged = EventObservable.Merge(eventsObs);
//
//                        var baseEventType = methodWithAttributes.observeBaseEventAttributes.First().BaseType;
//                        var observeBaseEvents = ObservableMergeMethodInfo.MakeGenericMethod(new Type[] { baseEventType });
//                        observeBaseEvents.Invoke(this, new object[] { methodWithAttributes.methodInfo, methodWithAttributes.observeBaseEventAttributes });
//                    }
                }
            }

            private object GetEventObservable(MethodInfo eventProcessorObserveMethod, ObserveEventAttribute observeEventAttribute)
            {
                // Here we create an action to pass to IEventObservable.Observe(action).
                // The body of this action will be another action calls the observe method (eventProcessorObserveMethod) on our _eventProcessor. 
                // The body action can support a number of different signatures. 
                //
                // We the created delegate to to our router:
                //  IDisposable disposable = _router.GetEventObservable<TEvent>().Observe(observeDelegate);
                //
                // Moving forward we should support passing interfaces the model implements if the observe target ask for it, i.e. 
                //  Action<TModel, TEvent, IEventContext> observeDelegate = (ISomethingImplementedByTModel m, TEvent e, IEventContext c) => eventProcessorObserve(m);

                EnsureObserveEventSignatureCorrect(eventProcessorObserveMethod, observeEventAttribute.EventType);

          

              //  MethodCallExpression onEventReceivedMethod = Expression.Call(Expression.Constant(_eventProcessor), eventProcessorObserveMethod, args);

                // Delegate observeDelegate = Expression.Lambda(onEventReceivedMethod, new ParameterExpression[] { @event, context, model }).Compile();

                var getEventObservableMethod = GetEventObservableMethodInfo.MakeGenericMethod(observeEventAttribute.EventType);
                object eventObservable = getEventObservableMethod.Invoke(_router, new object[] { observeEventAttribute.Stage });

                return eventObservable;
                //                var observeMethod = eventObservable.GetType().GetMethod("Observe", new Type[] { observeDelegate.GetType()});
                //                var disposable = observeMethod.Invoke(eventObservable, new[] { observeDelegate });
                //                AddDisposable((IDisposable)disposable);
            }

//            private void ObserveBaseEvents<TBaseEvent>(MethodInfo method, ObserveBaseEventAttribute[] observeEventAttributes)
//            {
//                var eventObservables = new IEventObservable<TBaseEvent, IEventContext, TModel>[observeEventAttributes.Length];
//                Type baseEventType = typeof(TBaseEvent);
//                EnsureObserveEventSignatureCorrect(method, baseEventType);
//                for (int i = 0; i < observeEventAttributes.Length; i++)
//                {
//                    ObserveBaseEventAttribute baseEventAttribute = observeEventAttributes[i];
//                    var baseEventsMatch = baseEventType == baseEventAttribute.BaseType;
//                    if (!baseEventsMatch)
//                    {
//                        throw new NotSupportedException("Base event types don't match");
//                    }
//                    eventObservables[i] = _router.GetEventObservable<TBaseEvent>(baseEventAttribute.Stage);
//                }
//                var eventObservable = EventObservable.Merge(eventObservables);
//                ObserveEvent(method, baseEventType, eventObservable);
//            }

            private void ObserveEvent(MethodInfo method, Type eventType, object eventObservable)
            {
                ParameterInfo[] parameters = method.GetParameters();

                ParameterExpression @event = Expression.Parameter(eventType, "@event");
                ParameterExpression context = Expression.Parameter(typeof(IEventContext), "context");
                ParameterExpression model = Expression.Parameter(typeof(TModel), "model");

                ParameterExpression[] args = new ParameterExpression[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    var parameterInfo = parameters[i];
                    if (parameterInfo.ParameterType == typeof(TModel))
                    {
                        args[i] = model;
                    }
                    else if (parameterInfo.ParameterType == eventType)
                    {
                        args[i] = @event;

                    }
                    else if (parameterInfo.ParameterType == typeof(IEventContext))
                    {
                        args[i] = context;
                    }
                }
//
//                MethodCallExpression onEventReceivedMethod = Expression.Call(Expression.Constant(_eventProcessor), method, args);
//
//                Delegate observeDelegate = Expression.Lambda(onEventReceivedMethod, new ParameterExpression[] { @event, context, model }).Compile();
//
//                //ObserveDelegate observeDelegate = CreateObserveDelegate(method, eventType);
//                var observeMethod = eventObservable.GetType().GetMethod("Observe", new Type[] { observeDelegate.ActionType });
//                var disposable = observeMethod.Invoke(eventObservable, new[] { observeDelegate.Delegate });
//                AddDisposable((IDisposable)disposable);
            }

            private void EnsureObserveEventSignatureCorrect(MethodInfo method, Type eventType)
            {
                ParameterInfo[] parameters = method.GetParameters();
                bool signatureCorrect = parameters.Length == 0;
                if (parameters.Length == 1)
                {
                    signatureCorrect =
                         parameters[0].ParameterType == typeof(TModel) ||
                         parameters[0].ParameterType == eventType ||
                         parameters[0].ParameterType == typeof(IEventContext);
                }
                if (parameters.Length == 2)
                {
                    signatureCorrect =
                        (parameters[0].ParameterType == eventType && parameters[1].ParameterType == typeof(TModel)) ||
                        (parameters[0].ParameterType == eventType && parameters[1].ParameterType == typeof(IEventContext));
                }
                else if (parameters.Length == 3)
                {
                    signatureCorrect =
                        parameters[0].ParameterType == eventType &&
                        parameters[1].ParameterType == typeof(IEventContext) &&
                        parameters[2].ParameterType == typeof(TModel);
                }
                if (!signatureCorrect)
                {
                    var message = string.Format(
                        "Incorrect ObserveEventAttribute usage on method {4}.{5}(). Expected a method with one of the following signatures:{0}void({1}, {2}, {3}){0}void({1}, {2})",
                        Environment.NewLine,
                        eventType.FullName,
                        typeof(IEventContext).FullName,
                        typeof(TModel).FullName,
                        method.DeclaringType.FullName,
                        method.Name
                    );
                    throw new InvalidOperationException(message);
                }
            }

            private ObserveDelegate CreateObserveDelegate(MethodInfo method, Type eventType)
            {
                ParameterInfo[] parameters = method.GetParameters();
                Type actionType;
                if (parameters.Length == 0)
                {
                    actionType = typeof(Action);
                }
                else if (parameters.Length == 1)
                {
                    if (parameters[0].ParameterType == typeof(TModel))
                    {
                        actionType = typeof(Action<>).MakeGenericType(new Type[] { typeof(TModel) });
                    }
                    else if (parameters[0].ParameterType == eventType)
                    {
                        actionType = typeof(Action<>).MakeGenericType(new Type[] { eventType });
                    }
                    else if (parameters[0].ParameterType == typeof(IEventContext))
                    {
                        actionType = typeof(Action<>).MakeGenericType(new Type[] { typeof(IEventContext) });
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
                else if (parameters.Length == 2)
                {
                    actionType = typeof(Action<,>).MakeGenericType(new Type[] { eventType, typeof(TModel) });
                }
                else if (parameters.Length == 3)
                {
                    actionType = typeof(Action<,,>).MakeGenericType(new Type[]
                        {eventType, typeof (IEventContext), typeof (TModel)});
                }
                else
                {
                    throw new NotSupportedException();
                }
                var @delegate = Delegate.CreateDelegate(actionType, _eventProcessor, method);
                return new ObserveDelegate(@delegate, actionType);
            }

            private class ObserveDelegate
            {
                public ObserveDelegate(Delegate @delegate, Type actionType)
                {
                    Delegate = @delegate;
                    ActionType = actionType;
                }

                public Delegate Delegate { get; private set; }

                public Type ActionType { get; private set; }
            }
        }
    }
}