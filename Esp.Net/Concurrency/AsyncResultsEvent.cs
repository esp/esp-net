using System;

#if ESP_EXPERIMENTAL
namespace Esp.Net.Concurrency
{
    public class AsyncResultsEvent<TResult>
    {
        public AsyncResultsEvent(TResult result, Guid id)
        {
            Result = result;
            Id = id;
        }

        public TResult Result { get; private set; }

        public Guid Id { get; private set; }
    }
}
#endif
