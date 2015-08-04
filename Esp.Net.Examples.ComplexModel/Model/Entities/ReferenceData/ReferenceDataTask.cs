using System;
using System.Reactive.Concurrency;
using Esp.Net.Examples.ComplexModel.Model.Events;

namespace Esp.Net.Examples.ComplexModel.Model.Entities.ReferenceData
{
    public class ReferenceDataTask : IReferenceDataTask
    {
        private readonly IRouter _router;
        private readonly IScheduler _scheduler;

        public ReferenceDataTask(IRouter router, IScheduler scheduler)
        {
            _router = router;
            _scheduler = scheduler;
        }

        public void BeginGetReferenceDataForCurrencyPair(Guid modelId, string currencyPair)
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(5), () =>
            {
                var refData = new CurrencyPairReferenceData(currencyPair, new[] { DateTime.Today, DateTime.Today.AddDays(1) });
                _router.PublishEvent(modelId, new CurrencyPairReferenceDataReceivedEvent(refData));
            });
        }
    }
}