#if NO_THREAD
namespace Esp.Net
{
    // taken from rx.net
    class Thread
    {
        private readonly ThreadStart _start;

        public Thread(ThreadStart start)
        {
            _start = start;
        }

        public string Name { get; set; }
        public bool IsBackground { get; set; }

        public void Start()
        {
            System.Threading.Tasks.Task.Factory.StartNew(Run, System.Threading.Tasks.TaskCreationOptions.LongRunning);
        }

        private void Run()
        {
            _start();
        }
    }

    delegate void ThreadStart();
}
#endif