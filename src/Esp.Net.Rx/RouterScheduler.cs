using System;
using System.Reactive;
using System.Reactive.Linq;
using Esp.Net.Utils;

namespace Esp.Net
{
    public static class ObservableExt
    {
        public static IDisposable SubscribeWithRouter<T, TModel>(this IObservable<T> source, IRouter<TModel> router, Action<TModel, T> onNext)
        {
            return source.SubscribeWithRouter(router, onNext, null, null);
        }

        public static IDisposable SubscribeWithRouter<T, TModel>(this IObservable<T> source, IRouter<TModel> router, Action<TModel, T> onNext, Action<TModel, Exception> onError)
        {
            return source.SubscribeWithRouter(router, onNext, onError, null);
        }

        public static IDisposable SubscribeWithRouter<T, TModel>(this IObservable<T> source, IRouter<TModel> router, Action<TModel, T> onNext, Action<TModel> onCompleted)
        {
            return source.SubscribeWithRouter(router, onNext, null, onCompleted);
        }

        public static IDisposable SubscribeWithRouter<T, TModel>(this IObservable<T> source, IRouter<TModel> router, Action<TModel, T> onNext, Action<TModel, Exception> onError, Action<TModel> onCompleted)
        {
            Guard.Requires<ArgumentNullException>(source != null, "source  can not be null");
            Guard.Requires<ArgumentNullException>(router != null, "router can not be null");
            Guard.Requires<ArgumentNullException>(onNext != null, "onNext can not be null");
            return source.Materialize().Subscribe(i =>
            {
                if (i.Kind == NotificationKind.OnError && onError == null) return;
                if (i.Kind == NotificationKind.OnCompleted && onCompleted == null) return;
                router.RunAction(model =>
                {
                    switch (i.Kind)
                    {
                        case NotificationKind.OnNext:
                            onNext(model, i.Value);
                            break;
                        case NotificationKind.OnError:
                            onError(model, i.Exception);
                            break;
                        case NotificationKind.OnCompleted:
                            onCompleted(model);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(string.Format("Unknown {0} '{1}'", typeof(NotificationKind).Name, i.Kind));
                    }
                });
            });
        }

        public static IDisposable SubscribeWithRouter<T, TModel>(this IObservable<T> source, IRouter<TModel> router, Action<T> onNext)
        {
            return source.SubscribeWithRouter(router, onNext, null, null);
        }

        public static IDisposable SubscribeWithRouter<T, TModel>(this IObservable<T> source, IRouter<TModel> router, Action<T> onNext, Action<Exception> onError)
        {
            return source.SubscribeWithRouter(router, onNext, onError, null);
        }

        public static IDisposable SubscribeWithRouter<T, TModel>(this IObservable<T> source, IRouter<TModel> router, Action<T> onNext, Action onCompleted)
        {
            return source.SubscribeWithRouter(router, onNext, null, onCompleted);
        }

        public static IDisposable SubscribeWithRouter<T, TModel>(this IObservable<T> source, IRouter<TModel> router, Action<T> onNext, Action<Exception> onError, Action onCompleted)
        {
            Guard.Requires<ArgumentNullException>(source != null, "source  can not be null");
            Guard.Requires<ArgumentNullException>(router != null, "router can not be null");
            Guard.Requires<ArgumentNullException>(onNext != null, "onNext can not be null");
            return source.Materialize().Subscribe(i =>
            {
                if (i.Kind == NotificationKind.OnError && onError == null) return;
                if (i.Kind == NotificationKind.OnCompleted && onCompleted == null) return;
                router.RunAction(model =>
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