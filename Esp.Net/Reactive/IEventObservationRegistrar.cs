namespace Esp.Net.Reactive
{
    public interface IEventObservationRegistrar
    {
        void IncrementRegistration<TEvent>();
        void DecrementRegistration<TEvent>();
    }
}