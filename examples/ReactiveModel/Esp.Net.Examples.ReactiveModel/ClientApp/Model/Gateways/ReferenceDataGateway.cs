using System;
using System.Linq;
using Esp.Net.Examples.ReactiveModel.ClientApp.Model.Entities;
using Esp.Net.Examples.ReactiveModel.ClientApp.Model.Events;
using Esp.Net.Examples.ReactiveModel.ClientApp.Services;
using Esp.Net.Examples.ReactiveModel.ClientApp.Services.Entities;

namespace Esp.Net.Examples.ReactiveModel.ClientApp.Model.Gateways
{
    public class ReferenceDataGateway : IReferenceDataGateway
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(ReferenceDataGateway));

        private readonly IRouter<OrderScreen> _router;
        private readonly IReferenceDataServiceClient _referenceDataServiceClient;

        public ReferenceDataGateway(IRouter<OrderScreen> router, IReferenceDataServiceClient referenceDataServiceClient)
        {
            _router = router;
            _referenceDataServiceClient = referenceDataServiceClient;
        }

        public IDisposable BeginGetReferenceData()
        {
            Log.Debug("Getting reference Data");
            return _referenceDataServiceClient.GetCurrencyPairs().Subscribe(currencyPairsDtos =>
            {
                Log.Debug("Reference Data received");
                CurrencyPair[] currencyPairs = currencyPairsDtos.Select(p => new CurrencyPair(p.IsoCode, p.Precision)).ToArray();
                _router.PublishEvent(new ReferenceDataReceivedEvent(currencyPairs));
            },
            ex =>
            {
                // TODO
            });
        }
    }
}