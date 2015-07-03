#if ESP_EXPERIMENTAL
namespace Esp.Net.HeldEvents
{
    public interface IEventHoldingStrategy<in TModel, in TEvent>
    {
        bool ShouldHold(TModel model, TEvent @event, IEventContext context);
        IEventDescription GetEventDescription(TModel model, TEvent @event);
    }
}
#endif