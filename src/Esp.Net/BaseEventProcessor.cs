using System;
using System.Collections.Generic;
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
//            var methods = from methodInfo in GetType().GetMethods()
//                    let attribute = methodInfo.GetCustomAttribute<ObserveEventAttribute>(true)
//                    where attribute != null 
//                    select new { methodInfo, attribute };
//            foreach (var tuple in methods)
//            {
//                GetEventObservableMethodInfo.MakeGenericMethod(new Type[] {tuple.attribute});
//                // get the generic overload 
//                // _router.GetEventObservable<>()
//            }
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