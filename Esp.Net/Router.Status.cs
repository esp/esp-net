namespace Esp.Net
{
    public partial class Router
    {
        private enum Status
        {
            Idle,
            PreEventProcessing,
            EventProcessorDispatch,
            PostProcessing,
            DispatchModelUpdates,
            Halted,
        }
    }
}