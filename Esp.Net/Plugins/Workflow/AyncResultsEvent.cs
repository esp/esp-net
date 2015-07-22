#if ESP_EXPERIMENTAL
using System;

namespace Esp.Net.Plugins.Workflow
{
    internal class AyncResultsEvent<TResult> : IIdentifiableEvent
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