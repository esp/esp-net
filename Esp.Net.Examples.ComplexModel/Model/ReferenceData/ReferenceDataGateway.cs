using System;
using System.Reactive.Concurrency;
using Esp.Net.Examples.ComplexModel.Model.Events;

namespace Esp.Net.Examples.ComplexModel.Model.ReferenceData
{
    public class ReferenceDataGateway : IReferenceDataGateway
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(ReferenceDataGateway));

        private readonly IRouter _router;
        private readonly IScheduler _scheduler;

        public ReferenceDataGateway(IRouter router, IScheduler scheduler)
        {
            _router = router;
            _scheduler = scheduler;
        }

        public void BeginGetReferenceDataForCurrencyPair(Guid modelId, string currencyPair)
        {
            Log.Debug("Getting reference Data");
            _scheduler.Schedule(TimeSpan.FromSeconds(5), () =>
            {
                Log.Debug("Reference Data received");
                var refData = new CurrencyPairReferenceData(currencyPair, new[] { DateTime.Today, DateTime.Today.AddDays(1) });
                _router.PublishEvent(modelId, new CurrencyPairReferenceDataReceivedEvent(refData));
            });
        }
    }
}