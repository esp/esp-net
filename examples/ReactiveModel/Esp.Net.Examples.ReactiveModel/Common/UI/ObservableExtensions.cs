using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reactive.Linq;

namespace Esp.Net.Examples.ReactiveModel.Common.UI
{
    public static class ObservableExtensions
    {
        public static IObservable<TValue> ObserveProperty<T, TValue>(
            this T source,
             Expression<Func<T, TValue>> propertyExpression
        )
            where T : INotifyPropertyChanged
        {
            return source.ObserveProperty(propertyExpression, false);
        }

        public static IObservable<TValue> ObserveProperty<T, TValue>(
            this T source,
            Expression<Func<T, TValue>> propertyExpression,
            bool observeInitialValue
        )
            where T : INotifyPropertyChanged
        {
            var mExpr = (MemberExpression)propertyExpression.Body;

            var getValue = propertyExpression.Compile();

            var observable = from evt in Observable
                    .FromEvent<PropertyChangedEventHandler, PropertyChangedEventArgs>
                    (h => (s, e) => h(e),
                    h => source.PropertyChanged += h,
                    h => source.PropertyChanged -= h)
                             where evt.PropertyName == mExpr.Member.Name
                             select getValue(source);

            if (observeInitialValue)
                return observable.Merge(Observable.Return(getValue(source)));

            return observable;
        }
    }
}