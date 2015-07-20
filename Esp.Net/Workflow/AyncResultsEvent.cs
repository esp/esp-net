#if ESP_EXPERIMENTAL
using System;
using Esp.Net.Model;

namespace Esp.Net.Workflow
{
    public class AyncResultsEvent<TResult> : IIdentifiableEvent
    {
        public AyncResultsEvent(TResult results, Guid id)
        {
            Result = results;
            Id = id;
        }

        public TResult Result { get; private set; }

        public Guid Id { get; private set; }

    }
}
#endif