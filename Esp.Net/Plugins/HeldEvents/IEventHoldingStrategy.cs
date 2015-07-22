#if ESP_EXPERIMENTAL
namespace Esp.Net.Plugins.HeldEvents
{
    public interface IEventHoldingStrategy<in TModel, in TEvent> where TEvent : IIdentifiableEvent
    {
        bool ShouldHold(TModel model, TEvent @event, IEventContext context);
        IEventDescription GetEventDescription(TModel model, TEvent @event);
    }

    public interface IEventHoldingStrategy<in TModel, in TEvent, in TBaseEvent> : IEventHoldingStrategy<TModel, TEvent>
        where TEvent : IIdentifiableEvent, TBaseEvent
    {
    }
}
#endif