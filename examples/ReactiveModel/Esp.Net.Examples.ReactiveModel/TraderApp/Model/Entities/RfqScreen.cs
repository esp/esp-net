using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using Esp.Net.Examples.ReactiveModel.TraderApp.Model.Events;
using Esp.Net.Examples.ReactiveModel.TraderApp.Model.Gateways;
using Esp.Net.Examples.ReactiveModel.TraderApp.Services.Entities;
using log4net;

namespace Esp.Net.Examples.ReactiveModel.TraderApp.Model.Entities
{
    public class RfqScreen 
        : DisposableBase
        , IPreEventProcessor
        , IPostEventProcessor
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(RfqScreen));
        private readonly IRouter<RfqScreen> _router;
        private readonly IRfqServiceGateway _rfqServiceGateway;
        private readonly Func<CurrencyPair, decimal, Guid, RfqDetails> _rfqDetailsFactory;
        private readonly SerialDisposable _rfqEventsDisposable = new SerialDisposable();
        private readonly List<RfqDetails> _rfqs = new List<RfqDetails>();
        private readonly Dictionary<Guid, RfqDetails> _rfqsById = new Dictionary<Guid, RfqDetails>();
        private readonly IReadOnlyCollection<RfqDetails> _readOnlyRfqs;
        private int _version;

        public RfqScreen(
            IRouter<RfqScreen> router, 
            IRfqServiceGateway rfqServiceGateway,
            Func<CurrencyPair, decimal, Guid, RfqDetails> rfqDetailsFactory  // container provided factory function
        )
        {
            _router = router;
            _rfqServiceGateway = rfqServiceGateway;
            _rfqDetailsFactory = rfqDetailsFactory;
            _readOnlyRfqs = new ReadOnlyCollection<RfqDetails>(_rfqs);

            AddDisposable(_rfqEventsDisposable);
        }

        public int Version
        {
            get { return _version; }
        }

        public RfqDetails this[Guid rfqId]
        {
            get
            {
                return _rfqsById[rfqId];
            }
        }

        public IReadOnlyCollection<RfqDetails> Rfqs
        {
            get { return _readOnlyRfqs; }
        }

        public void ObserveEvents()
        {
            _router.ObserveEventsOn(this);
        }

        void IPreEventProcessor.Process()
        {
            _version++;
            Log.DebugFormat("Model version is at {0}", _version);
        }

        void IPostEventProcessor.Process()
        {
            foreach (RfqDetails rfqDetailse in _rfqs)
            {
                rfqDetailse.OnPostProcessing();
            }
        }

        [ObserveEvent(typeof(InitialiseEvent))]
        private void OnInitialiseEvent()
        {
            _rfqEventsDisposable.Disposable = _rfqServiceGateway.BeginReceiveRfqEvents();
        }

        [ObserveEvent(typeof(RfqReceivedEvent))]
        private void OnRfqReceivedEvent(RfqReceivedEvent e)
        {
            RfqDetails rfqDetails = _rfqDetailsFactory(e.Request.CurrencyPair, e.Request.Notional, e.Request.QuoteId);
            IDisposable eventSubscriptions = _router.ObserveEventsOn(rfqDetails); // in this example we just hold onto rfqDetails for ever, never dispose
            // in this example we just hold onto rfqDetails for ever, never dispose
            _rfqs.Insert(0, rfqDetails);
            _rfqsById.Add(rfqDetails.QuoteId, rfqDetails);
        }
    }
}