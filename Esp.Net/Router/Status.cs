namespace Esp.Net.Router
{
    internal enum Status
    {
        Idle,
        PreEventProcessing,
        EventProcessorDispatch,
        PostProcessing,
        DispatchModelUpdates,
        Halted,
    }
}