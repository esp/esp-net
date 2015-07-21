using System;

namespace Esp.Net.Router
{
    public interface IEventPublisher
    {
        void PublishEvent<TEvent>(Guid modelId, TEvent @event);
    }
}