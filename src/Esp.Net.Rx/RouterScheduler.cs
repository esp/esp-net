using System;
using System.Reactive;
using System.Reactive.Linq;

namespace Esp.Net
{
    public static class RxExt
    {
        public static IDisposable SubscribeOnRouter<T, TModel>(this IObservable<T> source, IRouter<TModel> router, Action<T> onNext)
        {
            return source.SubscribeOnRouter(router, new AnonymousObserver<T>(onNext));
        }

        public static IDisposable SubscribeOnRouter<T, TModel>(this IObservable<T> source, IRouter<TModel> router, Action<T> onNext, Action<Exception> onError)
        {
            return source.SubscribeOnRouter(router, new AnonymousObserver<T>(onNext, onError));
        }

        public static IDisposable SubscribeOnRouter<T, TModel>(this IObservable<T> source, IRouter<TModel> router, Action<T> onNext, Action<Exception> onError, Action onCompleted)
        {
            return source.SubscribeOnRouter(router, new AnonymousObserver<T>(onNext, onError, onCompleted));
        }

        public static IDisposable SubscribeOnRouter<T, TModel>(this IObservable<T> source, IRouter<TModel> router, IObserver<T> observer)
        {
            return source.Materialize().Subscribe(i =>
                {
                    router.RunAction(() =>
                    {
                        switch (i.Kind)
                        {
                            case NotificationKind.OnNext:
                                observer.OnNext(i.Value);
                                break;
                            case NotificationKind.OnError:
                                observer.OnError(i.Exception);
                                break;
                            case NotificationKind.OnCompleted:
                                observer.OnCompleted();
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(string.Format("Unknown {0} '{1}'", typeof(NotificationKind).Name, i.Kind));
                        }
                    });
                });
        }
    }
}