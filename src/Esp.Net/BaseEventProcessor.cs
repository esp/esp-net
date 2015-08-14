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
        private static readonly MethodInfo GetEventObservableMethodInfo = ReflectionHelper.GetGenericMethodByArgumentCount(typeof(IEventSubject<TModel>), "GetEventObservable", 1, 1);

        private readonly IRouter<TModel> _router;

        protected BaseModelEventProcessor(IRouter<TModel> router)
        {
            _router = router;
        }

        public void ObserveEvents()
        {
            // TODO all this functionality this should be weaved in, not done using reflection
            var methods = from methodInfo in GetType().GetMethods()
                    let attribute = methodInfo.GetCustomAttribute<ObserveEventAttribute>(true)
                    where attribute != null 
                    select new { methodInfo, attribute };
            foreach (var tuple in methods)
            {
                var getEventObservableMethod = GetEventObservableMethodInfo.MakeGenericMethod(tuple.attribute.EventType);
                object eventObservable = getEventObservableMethod.Invoke(_router, new object[] {tuple.attribute.Stage});
                ParameterInfo[] parameters  = tuple.methodInfo.GetParameters();
                Delegate d = null;
                MethodInfo observeMethod;
                if (parameters.Length == 2)
                {
                    var action = typeof(Action<,>).MakeGenericType(new Type[] { typeof(TModel), tuple.attribute.EventType});
                    d = Delegate.CreateDelegate(action, this, tuple.methodInfo);
                    observeMethod = eventObservable.GetType().GetMethod("Observe", new Type[] { action });
                } 
                else if (parameters.Length == 3)
                {
                    var action = typeof(Action<,,>).MakeGenericType(new Type[] { typeof(TModel), tuple.attribute.EventType, typeof(IEventContext) });
                    d = Delegate.CreateDelegate(action, this, tuple.methodInfo);
                    observeMethod = eventObservable.GetType().GetMethod("Observe", new Type[] { action });
                }
                else
                {
                    throw new NotSupportedException();
                }
                var disposable = observeMethod.Invoke(eventObservable, new[] {d});
                AddDisposable((IDisposable)disposable);
            }
        }

        protected void ObserveEvent<TEvent>(Action<TModel, TEvent, IEventContext> observer, ObservationStage stage = ObservationStage.Normal)
        {
            AddDisposable(_router.GetEventObservable<TEvent>(stage).Observe(observer));
        }

        protected void ObserveEvent<TEventBase>(Action<TModel, TEventBase, IEventContext> observer, params IEventObservable<TModel, TEventBase, IEventContext>[] observables)
        {
            var stream = EventObservable.Concat(observables);
            AddDisposable(stream.Observe(observer));
        }
    }
}