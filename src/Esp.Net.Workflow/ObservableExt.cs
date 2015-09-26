using System.Reactive.Concurrency;

namespace Esp.Net
{
    public static class ObservableExt
    {
        //public static System.IObservable<T>  
        public static IScheduler GetScheduler<TModel>(this IRouter<TModel> router)
        {
            return new RouterScheduler<TModel>(router);
        } 
    }
}