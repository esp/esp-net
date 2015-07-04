using System;

#if ESP_EXPERIMENTAL
namespace Esp.Net.Concurrency
{
    public abstract class IdentifiableEvent
    {
        protected IdentifiableEvent(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; private set; }
    }

    public class AyncResultsEvent<TResult> : IdentifiableEvent
    {
        public AyncResultsEvent(TResult results, Guid id) : base(id)
        {
            Result = results;
        }

        public TResult Result { get; private set; }
    }
}
#endif
