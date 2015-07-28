namespace Esp.Net
{
    public interface IEventContext
    {
        bool IsCanceled { get; }
        bool IsCommitted { get; }
        void Cancel();
        void Commit();
    }
}