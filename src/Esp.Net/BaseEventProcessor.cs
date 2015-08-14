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
#if ESP_EXPERIMENTAL
using System;
using System.Linq;
using System.Reflection;
using Esp.Net.Disposables;
using Esp.Net.ModelRouter;
using Esp.Net.Reactive;
using Esp.Net.Utils;

namespace Esp.Net
{
    public abstract class BaseModelEventProcessor<TModel> : DisposableBase
    {
        private static readonly MethodInfo GetEventObservableMethodInfo = ReflectionHelper.GetGenericMethodByArgumentCount(typeof (IEventSubject<TModel>), "GetEventObservable", 1, 1);
        private static readonly MethodInfo ObserveBaseEventsMethodInfo = ReflectionHelper.GetGenericMethodByArgumentCount(typeof (BaseModelEventProcessor<TModel>), "ObserveBaseEvents", 1, 2, BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly IRouter<TModel> _router;

        protected BaseModelEventProcessor(IRouter<TModel> router)
        {
            _router = router;
        }

        public virtual void ObserveEvents()
        {
            // TODO all this functionality this should be weaved in rather than using reflection
            var methodsWithAttributes =
                from methodInfo in GetType().GetMethods()
                let observeEventAttribute = methodInfo.GetCustomAttribute<ObserveEventAttribute>(true)
                let observeBaseEventAttributes = methodInfo.GetCustomAttributes<ObserveBaseEventAttribute>(true).ToArray()
                where observeEventAttribute != null || observeBaseEventAttributes.Length > 0
                select new { methodInfo, observeEventAttribute, observeBaseEventAttributes };
            foreach (var methodWithAttributes in methodsWithAttributes)
            {
                if (methodWithAttributes.observeEventAttribute != null)
                {
                    ObserveEvents(methodWithAttributes.methodInfo, methodWithAttributes.observeEventAttribute);
                }
                else if (methodWithAttributes.observeBaseEventAttributes.Any())
                {
                    var baseEventType = methodWithAttributes.observeBaseEventAttributes.First().BaseType;
                    var observeBaseEvents = ObserveBaseEventsMethodInfo.MakeGenericMethod(new Type[] {baseEventType});
                    observeBaseEvents.Invoke(this, new object[] { methodWithAttributes.methodInfo, methodWithAttributes.observeBaseEventAttributes });
                }
            }
        }

        protected void ObserveEvent<TEvent>(Action<TModel, TEvent, IEventContext> observer,
            ObservationStage stage = ObservationStage.Normal)
        {
            AddDisposable(_router.GetEventObservable<TEvent>(stage).Observe(observer));
        }

        protected void ObserveEvent<TEventBase>(Action<TModel, TEventBase, IEventContext> observer,
            params IEventObservable<TModel, TEventBase, IEventContext>[] observables)
        {
            var stream = EventObservable.Concat(observables);
            AddDisposable(stream.Observe(observer));
        }

        private void ObserveEvents(MethodInfo method, ObserveEventAttribute observeEventAttribute)
        {
            var getEventObservableMethod = GetEventObservableMethodInfo.MakeGenericMethod(observeEventAttribute.EventType);
            object eventObservable = getEventObservableMethod.Invoke(_router, new object[] {observeEventAttribute.Stage});
            EnsureObserveEventSignatureCorrect(method, observeEventAttribute.EventType);
            ObserveEvent(method, observeEventAttribute.EventType, eventObservable);
        }

        private void ObserveBaseEvents<TBaseEvent>(MethodInfo method, ObserveBaseEventAttribute[] observeEventAttributes)
        {
            var eventObservables = new IEventObservable<TModel, TBaseEvent, IEventContext>[observeEventAttributes.Length];
            Type baseEventType = typeof(TBaseEvent);
            EnsureObserveEventSignatureCorrect(method, baseEventType);
            for (int i = 0; i < observeEventAttributes.Length; i++)
            {
                ObserveBaseEventAttribute baseEventAttribute = observeEventAttributes[i];
                var baseEventsMatch = baseEventType == baseEventAttribute.BaseType;
                if (!baseEventsMatch)
                {
                    throw new NotSupportedException("Base event types don't match");
                }
                eventObservables[i] = _router.GetEventObservable<TBaseEvent>(baseEventAttribute.EventType, baseEventAttribute.Stage);
            }
            var eventObservable = EventObservable.Concat(eventObservables);
            ObserveEvent(method, baseEventType, eventObservable);
        }

        private void ObserveEvent(MethodInfo method, Type baseEventType, object eventObservable)
        {
            ObserveDelegate observeDelegate = CreateObserveDelegate(method, baseEventType);
            var observeMethod = eventObservable.GetType().GetMethod("Observe", new Type[] {observeDelegate.ActoinType});
            var disposable = observeMethod.Invoke(eventObservable, new[] {observeDelegate.Delegate});
            AddDisposable((IDisposable) disposable);
        }

        private void EnsureObserveEventSignatureCorrect(MethodInfo method, Type eventType)
        {
            ParameterInfo[] parameters = method.GetParameters();
            bool signatureCorrect = false;
            if (parameters.Length == 2)
            {
                signatureCorrect =
                    parameters[0].ParameterType == typeof(TModel) &&
                    parameters[1].ParameterType == eventType;
            }
            else if (parameters.Length == 3)
            {
                signatureCorrect =
                    parameters[0].ParameterType == typeof(TModel) &&
                    parameters[1].ParameterType == eventType &&
                    parameters[2].ParameterType == typeof(IEventContext);
            }
            if (!signatureCorrect)
            {
                var message = string.Format(
                    "Incorrect ObserveEventAttribute usage on method {4}.{5}(). Expected a method with one of the following signatures:{0}void({1}, {2}, {3}){0}void({1}, {2})",
                    Environment.NewLine,
                    typeof(TModel).FullName,
                    eventType.FullName,
                    typeof(IEventContext).FullName,
                    method.DeclaringType.FullName,
                    method.Name
                    );
                throw new InvalidOperationException(message);
            }
        }

        private ObserveDelegate CreateObserveDelegate(MethodInfo method, Type eventType)
        {
            ParameterInfo[] parameters = method.GetParameters();
            Delegate @delegate = null;
            Type actionType;
            if (parameters.Length == 2)
            {
                actionType =
                    typeof(Action<,>).MakeGenericType(new Type[] { typeof(TModel), eventType });
                @delegate = Delegate.CreateDelegate(actionType, this, method);
            }
            else if (parameters.Length == 3)
            {
                actionType =
                    typeof(Action<,,>).MakeGenericType(new Type[]
                    {typeof (TModel), eventType, typeof (IEventContext)});
                @delegate = Delegate.CreateDelegate(actionType, this, method);
            }
            else
            {
                throw new NotSupportedException();
            }
            return new ObserveDelegate(@delegate, actionType);
        }

        private class ObserveDelegate
        {
            public ObserveDelegate(Delegate @delegate, Type actoinType)
            {
                Delegate = @delegate;
                ActoinType = actoinType;
            }

            public Delegate Delegate { get; private set; }

            public Type ActoinType { get; private set; }
        }
    }
}
#endif