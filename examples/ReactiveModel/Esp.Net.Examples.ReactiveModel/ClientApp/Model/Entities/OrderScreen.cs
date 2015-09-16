using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using Esp.Net.Examples.ReactiveModel.ClientApp.Model.Events;
using Esp.Net.Examples.ReactiveModel.ClientApp.Model.Gateways;
using Esp.Net.Examples.ReactiveModel.ClientApp.Services.Entities;
using log4net;

namespace Esp.Net.Examples.ReactiveModel.ClientApp.Model.Entities
{
    public class OrderScreen 
        : DisposableBase
        , IPreEventProcessor
        , IPostEventProcessor
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(OrderScreen));
        private readonly IRouter<OrderScreen> _router;
        private readonly IReferenceDataGateway _referenceDataGateway;
        private readonly OrderInputs.OrderInputs _orderInputs;
        private readonly Rfq.Rfq _rfq;
        private readonly SerialDisposable _referenceDataDisposable = new SerialDisposable();
        private IReadOnlyCollection<CurrencyPair> _currentyPairs;
        private int _version;

        public OrderScreen(
            IRouter<OrderScreen> router, 
            IReferenceDataGateway referenceDataGateway, 
            OrderInputs.OrderInputs orderInputs,
            Rfq.Rfq rfq)
        {
            _router = router;
            _referenceDataGateway = referenceDataGateway;
            _orderInputs = orderInputs;
            _rfq = rfq;
            AddDisposable(_referenceDataDisposable);
        }

        public int Version
        {
            get { return _version; }
        }

        public IReadOnlyCollection<CurrencyPair> CurrencyPairs
        {
            get { return _currentyPairs; }
        }

        public OrderInputs.OrderInputs Inputs
        {
            get { return _orderInputs; }
        }

        public Rfq.Rfq Rfq
        {
            get { return _rfq; }
        }

        public void ObserveEvents()
        {
            _router.ObserveEventsOn(this);
            _router.ObserveEventsOn(_orderInputs);
            _router.ObserveEventsOn(_rfq);
        }

        void IPreEventProcessor.Process()
        {
            _version++;
            Log.DebugFormat("Model version is at {0}", _version);
        }

        void IPostEventProcessor.Process()
        {
            _rfq.OnPostProcessing();
            _orderInputs.OnPostProcessing(this);
        }

        [ObserveEvent(typeof(ReferenceDataReceivedEvent))]
        private void OnReferenceDataReceivedEvent(ReferenceDataReceivedEvent e, IEventContext context)
        {
            Log.DebugFormat("Applying reference data");
            _currentyPairs = new ReadOnlyCollection<CurrencyPair>(e.CurrencyPairs);
            context.Commit();
        }

        [ObserveEvent(typeof(InitialiseEvent))]
        private void OnInitialiseEvent()
        {
            _referenceDataDisposable.Disposable = _referenceDataGateway.BeginGetReferenceData();
        }
    }
}