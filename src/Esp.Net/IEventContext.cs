namespace Esp.Net
{
    public interface IEventContext
    {
        ObservationStage CurrentStage { get; }
        bool IsCanceled { get; }
        bool IsCommitted { get; }
        void Cancel();
        void Commit();
    }
}