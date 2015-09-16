using System.Reactive.Concurrency;

namespace Esp.Net.Examples.ReactiveModel.Common.Services
{
    public interface ISchedulerService
    {
        IScheduler ThreadPool { get; } 
        IScheduler Ui { get; } 
    }

    public class SchedulerService : ISchedulerService
    {
        public SchedulerService()
        {
            ThreadPool = ThreadPoolScheduler.Instance;
            Ui = DispatcherScheduler.Instance;
        }

        public IScheduler ThreadPool { get; private set; }

        public IScheduler Ui { get; private set; }
    }
}