namespace Esp.Net.Reactive
{
    internal interface IEventObservationRegistrar
    {
        void IncrementRegistration<TEvent>();
        void DecrementRegistration<TEvent>();
    }
}