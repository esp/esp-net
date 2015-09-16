using System;
using Esp.Net.Examples.ReactiveModel.Common.Model.Entities.Fields;
using Esp.Net.Examples.ReactiveModel.TraderApp.Model.Events;
using Esp.Net.Examples.ReactiveModel.TraderApp.Model.Gateways;
using Esp.Net.Examples.ReactiveModel.TraderApp.Services.Entities;

namespace Esp.Net.Examples.ReactiveModel.TraderApp.Model.Entities
{
    public class RfqDetails : DisposableBase
    {
        private readonly Field<decimal?> _rate;
        private readonly decimal _notional;
        private readonly CurrencyPair _currencyPair;
        private readonly Guid _quoteId;
        private readonly IRfqServiceGateway _rfqServiceGateway;
        private QuoteStatus _quoteStatus;
        private string _rfqSummary;

        public QuoteStatus QuoteStatus
        {
            get { return _quoteStatus; }
        }

        public CurrencyPair CurrencyPair
        {
            get { return _currencyPair; }
        }

        public decimal Notional
        {
            get { return _notional; }
        }

        public IField<decimal?> Rate
        {
            get { return _rate; }
        }

        public string RfqSummary
        {
            get { return _rfqSummary; }
        }

        public Guid QuoteId
        {
            get { return _quoteId; }
        }

        public RfqDetails(CurrencyPair currencyPair, decimal notional, Guid quoteId, IRfqServiceGateway rfqServiceGateway)
        {
            _currencyPair = currencyPair;
            _notional = notional;
            _quoteId = quoteId;
            _rfqServiceGateway = rfqServiceGateway;
            _rate = new Field<decimal?>();
            _quoteStatus = QuoteStatus.New;
        }

        public void OnPostProcessing()
        {
            _rfqSummary = string.Format(
                "You SELL {0} {1} against {2} at {3}",
                _notional,
                _currencyPair.Base,
                _currencyPair.Counter,
                _rate.HasValue ? _rate.Value.ToString() : "[Enter Rate]"
            );
        }

        [ObserveEvent(typeof(RfqRateChangedEvent))]
        private void OnRfqRateChangedEvent(RfqRateChangedEvent e)
        {
            if(e.RfqCorrelationId != _quoteId) return;
            _rate.Value = e.Rate;
        }

        [ObserveEvent(typeof(ClientAcceptedQuoteEvent))]
        private void OnClientAcceptedQuoteEvent(ClientAcceptedQuoteEvent e)
        {
            if(e.QuoteId != _quoteId) return;
            _quoteStatus = QuoteStatus.Booked;
            _rfqServiceGateway.SendUpdate(this, true);
        }

        [ObserveEvent(typeof(ClientRejectQuoteEvent))]
        private void OnClientRejectQuoteEvent(ClientRejectQuoteEvent e)
        {
            if (e.QuoteId != _quoteId) return;
            _quoteStatus = QuoteStatus.ClientRejected;
            _rfqServiceGateway.SendUpdate(this, true);
        }

        [ObserveEvent(typeof(ClientClosedQuoteEvent))]
        private void OnClientClosedQuoteEvent(ClientClosedQuoteEvent e)
        {
            if (e.QuoteId != _quoteId) return;
            var isAlreadyAtEndState = _quoteStatus == QuoteStatus.Booked || _quoteStatus == QuoteStatus.ClientRejected || _quoteStatus == QuoteStatus.TraderRejected;
            if(!isAlreadyAtEndState)
                _quoteStatus = QuoteStatus.ClientRejected;
        }

        [ObserveEvent(typeof(TraderSendQuoteEvent))]
        private void OnTraderSendQuoteEvent(TraderSendQuoteEvent e)
        {
            if (e.QuoteId != _quoteId) return;
            _quoteStatus = QuoteStatus.Quoting;
            _rfqServiceGateway.SendUpdate(this);
        }

        [ObserveEvent(typeof(TraderRejectQuoteEvent))]
        private void OnTraderRejectQuoteEvent(TraderRejectQuoteEvent e)
        {
            if (e.QuoteId != _quoteId) return;
            _quoteStatus = QuoteStatus.TraderRejected;
            _rate.Value = null;
            _rfqServiceGateway.SendUpdate(this);
        }
    }
}