using System;
using System.Reactive;
using System.Reactive.Linq;
using Esp.Net.Utils;

namespace Esp.Net
{
    public static class ObservableExt
    {
        public static IDisposable Subscribe<T, TModel>(this IObservable<T> source, IRouter<TModel> router, Action<T> onNext)
        {
            return source.Subscribe(router, onNext, null, null);
        }

        public static IDisposable Subscribe<T, TModel>(this IObservable<T> source, IRouter<TModel> router, Action<T> onNext, Action<Exception> onError)
        {
            return source.Subscribe(router, onNext, onError, null);
        }

        public static IDisposable Subscribe<T, TModel>(this IObservable<T> source, IRouter<TModel> router, Action<T> onNext, Action onCompleted)
        {
            return source.Subscribe(router, onNext, null, onCompleted);
        }

        public static IDisposable Subscribe<T, TModel>(this IObservable<T> source, IRouter<TModel> router, Action<T> onNext, Action<Exception> onError, Action onCompleted)
        {
            // After looking into several methods to get an rx stream to be observed on the dispatcher loop, this Subscribe overload is the best I came up with. 
            // 
            // Firstly I checkout out implementing an IScheduler so you could just use .ObserveOn(routerScheduler).
            // However the scheduler seems to be invoked at other times, not just to call IObserver actions, the behavior was a bit strange but I parked the investigation for the next reason. 
            // If you don't have an OnCompleted or OnError handler on your stream, you don't want to schedule an action on the routers dispatch loop, there is no way to figure that out with an IScheduler implementation. 
            // Along similar lines, you can't just create a ObserveOn ext methods and have it take the router (like this one). 
            // This is because you have no control over the IObserver you'll be given from up stream.
            // The AnonymousObserver you'll be given will always have stubbed onComplted and onError handlers, e.g. if you're strem has no OnComplete delegate, the AnonymousObserver will default it with a noop one.
            // It boils down to you not being able to check if the OnCompleted/OnError delegates are they're there (like we do here), and only schedule an action if so.

            Guard.Requires<ArgumentNullException>(source != null, "source  can not be null");
            Guard.Requires<ArgumentNullException>(router != null, "router can not be null");
            Guard.Requires<ArgumentNullException>(onNext != null, "onNext can not be null");
            return source.Materialize().Subscribe(i =>
                {
                    if(i.Kind == NotificationKind.OnError && onError == null) return;
                    if(i.Kind == NotificationKind.OnCompleted && onCompleted == null) return;
                    router.RunAction(() =>
                    {
                        switch (i.Kind)
                        {
                            case NotificationKind.OnNext:
                                onNext(i.Value);
                                break;
                            case NotificationKind.OnError:
                                onError(i.Exception);
                                break;
                            case NotificationKind.OnCompleted:
                                onCompleted();
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(string.Format("Unknown {0} '{1}'", typeof(NotificationKind).Name, i.Kind));
                        }
                    });
                });
        }
    }
}