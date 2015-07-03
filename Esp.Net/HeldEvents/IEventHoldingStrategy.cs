namespace Esp.Net.HeldEvents
{
    public interface IEventHoldingStrategy<in TModel, in TEvent, in TContext>
    {
        bool ShouldHold(TModel model, TEvent @event, TContext context);
        IEventDescription GetEventDescription(TModel model, TEvent @event);
    }
}