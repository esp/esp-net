using System;
using System.Reactive.Concurrency;
using Esp.Net.Examples.ComplexModel.Model.Events;
using Esp.Net.Examples.ComplexModel.Model.Snapshot.Schedule;

namespace Esp.Net.Examples.ComplexModel.Model.Schedule
{
    public class ScheduleGenerationGateway : IScheduleGenerationGateway
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(ScheduleGenerationGateway));

        private readonly IEventPublisher _eventPublisher;
        private readonly IScheduler _scheduler;

        public ScheduleGenerationGateway(IEventPublisher eventPublisher, IScheduler scheduler)
        {
            _eventPublisher = eventPublisher;
            _scheduler = scheduler;
        }

        public void BeginGenerateSchedule(Guid modelId, string currencyPair, FixingFrequency value)
        {
            Log.Debug("Getting schedule Data");
            _scheduler.Schedule(TimeSpan.FromSeconds(2), () =>
            {
                Log.Debug("Schedule received");
                var holidayDates = new[] { DateTime.Today.AddDays(7), DateTime.Today.AddDays(14) };
                _eventPublisher.PublishEvent(
                    modelId, 
                    new ScheduleResolvedEvent(new[]
                    {
                        new CouponSnapshot(Guid.NewGuid(), 500m, DateTime.Today.AddDays(1), holidayDates), 
                        new CouponSnapshot(Guid.NewGuid(), 500m, DateTime.Today.AddDays(1), holidayDates), 
                        new CouponSnapshot(Guid.NewGuid(), 500m, DateTime.Today.AddDays(1), holidayDates),
                    })
                );
            });
        }
    }
}