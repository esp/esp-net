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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
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
            private static readonly MethodInfo GetEventObservableMethodInfo = ReflectionHelper.GetGenericMethodByArgumentCount(typeof(IRouter<TModel>), "GetEventObservable", 1, 1);
            private static readonly MethodInfo ObservableMergeMethodInfo = ReflectionHelper.GetGenericMethodByArgumentCount(
                typeof(EventObservable),
                "Merge",
                3,
                1,
                BindingFlags.Public | BindingFlags.Static,
                // this should match the IEnumerable<IEventObservable<TEvent, TContext, TModel>> overload
                p => typeof(IEnumerable).IsAssignableFrom(p[0].ParameterType) && !p[0].ParameterType.IsArray
            );

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
                    ObserveEventAttribute[] observeEventAttributes = methodWithAttributes.observeEventAttributes;
                    MethodInfo method = methodWithAttributes.methodInfo;

                    if (observeEventAttributes.Length == 1)
                    {
                        ObserveEventAttribute observeEventAttribute = observeEventAttributes[0];
                        EnsureObserveEventSignatureCorrect(method, observeEventAttribute.EventType);
                        var eventObservable = CreateEventObservable(observeEventAttribute);
                        var observeDelegate = CreateEventObserverDelegate(method, observeEventAttribute.EventType);
                        ObserveEvent(eventObservable, observeDelegate);
                    }
                    else
                    {
                        var baseEventType = GetBaseEventType(method, observeEventAttributes);
                        EnsureObserveEventSignatureCorrect(method, baseEventType);
                        var eventObservable = CreateEventObservable(baseEventType, observeEventAttributes);
                        var observeDelegate = CreateEventObserverDelegate(method, baseEventType);
                        ObserveEvent(eventObservable, observeDelegate);
                    }
                }
            }

            // gets an IEventObservable<TEvent, TContext, TModel> for the details in the given attribute
            private object CreateEventObservable(ObserveEventAttribute observeEventAttribute)
            {
                var getEventObservableMethod = GetEventObservableMethodInfo.MakeGenericMethod(observeEventAttribute.EventType);
                return getEventObservableMethod.Invoke(_router, new object[] { observeEventAttribute.Stage });
            }

            // gets a merged IEventObservable<TEvent, TContext, TModel> for all events in the given attributes, the merged streams TEvent is of baseEventType
            private object CreateEventObservable(Type baseEventType, ObserveEventAttribute[] observeEventAttributes)
            {
                var eventObservableType = typeof(IEventObservable<,,>).MakeGenericType(new Type[] { baseEventType, typeof(IEventContext), typeof(TModel) });
                var eventObservables = (IList)typeof(List<>)
                    .MakeGenericType(eventObservableType)
                    .GetConstructor(new Type[]{})
                    .Invoke(null);
                for (int i = 0; i < observeEventAttributes.Length; i++)
                {
                    ObserveEventAttribute observeEventAttribute = observeEventAttributes[i];
                    dynamic eventObservable = CreateEventObservable(observeEventAttribute);
                    eventObservables.Add(eventObservable);
                }
                MethodInfo closedEventObservableMergeMethod = ObservableMergeMethodInfo.MakeGenericMethod(new Type[] { baseEventType, typeof(IEventContext), typeof(TModel) });
                return closedEventObservableMergeMethod.Invoke(null, new object[] { eventObservables });
            }

            // gets the Type of event which is either:
            //  a) type type of event in the given methodWithAttributes, note this must be common amongst all events from the observeEventAttributes
            //  b) if no event in the methodWithAttributes, get the type which is common amongst all events from the observeEventAttributes
            private Type GetBaseEventType(MethodInfo methodWithAttributes, ObserveEventAttribute[] observeEventAttributes)
            {
                // try find a param that's not the model, or IEventContext, i.e. an event,
                // we'll use that as the lowest base type when invoking EventObservable.Merge<TEvent>(obs)
                Type baseEventType = null;
                foreach (ParameterInfo parameterInfo in methodWithAttributes.GetParameters())
                {
                    if (typeof (TModel).IsAssignableFrom(parameterInfo.GetType()) ||
                        typeof (IEventContext).IsAssignableFrom(parameterInfo.GetType()))
                        continue;
                    baseEventType = parameterInfo.ParameterType;
                    break;
                }
                if (baseEventType != null)
                {
                    var shareSameBaseType = ReflectionHelper.SharesBaseType(baseEventType, Enumerable.Select<ObserveEventAttribute, Type>(observeEventAttributes, a => a.EventType));
                    if (!shareSameBaseType) ThrowOnInvalidBaseType(methodWithAttributes, observeEventAttributes);
                }
                else
                {
                    ReflectionHelper.TryGetCommonBaseType(out baseEventType, Enumerable.Select<ObserveEventAttribute, Type>(observeEventAttributes, a => a.EventType));
                }
                if (baseEventType == null) ThrowOnInvalidBaseType(methodWithAttributes, observeEventAttributes);
                return baseEventType;
            }

            private void ThrowOnInvalidBaseType(MethodInfo declaringMethod, ObserveEventAttribute[] observeEventAttributes)
            {
                var message = new StringBuilder("Could not determine a common base event type for events declared using the ObserveEvent attribute on method ");
                message.AppendFormat("[{0}.{1}]", declaringMethod.DeclaringType.FullName, declaringMethod.Name);
                message.AppendLine(".");
                message.AppendLine(" Events that don't share a base type:");
                foreach (ObserveEventAttribute attribute in observeEventAttributes)
                {
                    message.AppendLine(attribute.EventType.FullName);
                }
                throw new InvalidOperationException(message.ToString());
            }

            // creates a delegate that matches the largest signature from IEventObservable.Observe, it will proxy 'method' passing the required args
            private Delegate CreateEventObserverDelegate(MethodInfo method, Type eventType)
            {
                ParameterInfo[] parameters = method.GetParameters();

                ParameterExpression @event = Expression.Parameter(eventType, "@event");
                ParameterExpression context = Expression.Parameter(typeof (IEventContext), "context");
                ParameterExpression model = Expression.Parameter(typeof (TModel), "model");

                ParameterExpression[] args = new ParameterExpression[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    var parameterInfo = parameters[i];
                    if (parameterInfo.ParameterType == typeof (TModel))
                    {
                        args[i] = model;
                    }
                    else if (parameterInfo.ParameterType.IsAssignableFrom(eventType))
                    {
                        args[i] = @event;
                    }
                    else if (parameterInfo.ParameterType == typeof (IEventContext))
                    {
                        args[i] = context;
                    }
                }
                MethodCallExpression onEventReceivedMethod = Expression.Call(Expression.Constant(_eventProcessor), method, args);
                Delegate observeDelegate = Expression.Lambda(onEventReceivedMethod, new ParameterExpression[] {@event, context, model}).Compile();
                return observeDelegate;
            }

            private void ObserveEvent(object eventObservable, Delegate observeDelegate)
            {
                var observeMethod = eventObservable.GetType().GetMethod("Observe", new Type[] { observeDelegate.GetType() });
                var disposable = observeMethod.Invoke(eventObservable, new[] { observeDelegate });
                AddDisposable((IDisposable)disposable);
            }

            private void EnsureObserveEventSignatureCorrect(MethodInfo method, Type eventType)
            {
                Type modelType = typeof (TModel);
                Type eventContextType = typeof (IEventContext);
                bool modelParamChecked = false, eventParamChecked = false, contextParamChecked = false;
                foreach (ParameterInfo parameterInfo in method.GetParameters())
                {
                    if (parameterInfo.ParameterType != typeof (object))
                    {
                        if (!modelParamChecked && parameterInfo.ParameterType.IsAssignableFrom(modelType))
                        {
                            modelParamChecked = true;
                            continue;
                        }
                        if (!contextParamChecked && parameterInfo.ParameterType.IsAssignableFrom(eventContextType))
                        {
                            contextParamChecked = true;
                            continue;
                        }
                        if (!eventParamChecked && parameterInfo.ParameterType.IsAssignableFrom(eventType))
                        {
                            eventParamChecked = true;
                            continue;
                        }
                    }
                    var message = string.Format(
                        "Incorrect ObserveEventAttribute usage on method {0}.{1}(). Expected a method which signatures contains no parameters OR one or more of the following (in any order): {2}, {3}, {4}",
                        method.DeclaringType.FullName,
                        method.Name,
                        eventType.FullName,
                        typeof(IEventContext).FullName,
                        typeof(TModel).FullName
                    );
                    throw new InvalidOperationException(message);
                }
            }
        }
    }
}