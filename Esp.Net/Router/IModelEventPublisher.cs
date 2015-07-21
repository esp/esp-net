namespace Esp.Net.Router
{
    public interface IModelEventPublisher
    {
        void PublishEvent<TEvent>(TEvent @event);
    }
}