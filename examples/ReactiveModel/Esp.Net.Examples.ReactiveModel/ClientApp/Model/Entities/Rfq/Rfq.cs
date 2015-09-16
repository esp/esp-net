using System;
using System.Reactive.Disposables;
using Esp.Net.Examples.ReactiveModel.ClientApp.Model.Events;
using Esp.Net.Examples.ReactiveModel.ClientApp.Model.Gateways;
using Esp.Net.Examples.ReactiveModel.ClientApp.Services.Entities;

namespace Esp.Net.Examples.ReactiveModel.ClientApp.Model.Entities.Rfq
{
    public class Rfq : DisposableBase
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(Rfq));
        private readonly IRequestForQuoteGateway _rfqGateway;
        private readonly SerialDisposable _orderDisposable = new SerialDisposable();
        private readonly SerialDisposable _acceptQuoteDisposable = new SerialDisposable();
        private readonly SerialDisposable _rejectQuoteDisposable = new SerialDisposable();
        private Guid _quoteId;
        private decimal? _rate;
        private QuoteStatus _status;
        private string _rfqSummary;
        private InFlightRfqQuoteDetails _inFlightRfqQuoteDetails;

        public Rfq(IRequestForQuoteGateway rfqGateway)
        {
            _rfqGateway = rfqGateway;

            AddDisposable(_orderDisposable);
            AddDisposable(_acceptQuoteDisposable);
            AddDisposable(_rejectQuoteDisposable);
        }

        public Guid QuoteId
        {
            get { return _quoteId; }
        }

        public decimal? Rate
        {
            get { return _rate; }
        }

        public QuoteStatus Status
        {
            get { return _status; }
        }

        public string RfqSummary
        {
            get { return _rfqSummary; }
        }

        [ObserveEvent(typeof(RequestQuoteEvent))]
        private void OnPlaceOrderEvent(OrderScreen model)
        {
            Log.DebugFormat("Beginning RFQ");
            _quoteId = Guid.NewGuid();
            _status = QuoteStatus.Requesting;
            _inFlightRfqQuoteDetails = new InFlightRfqQuoteDetails
            {
                QuoteId = _quoteId,
                CurrencyPair = model.Inputs.CurrencyPair.Value,
                Notional = model.Inputs.Notional.Value.Value
            };
            _orderDisposable.Disposable = _rfqGateway.BegingGetQuote(
                _inFlightRfqQuoteDetails.QuoteId,
                _inFlightRfqQuoteDetails.CurrencyPair,
                _inFlightRfqQuoteDetails.Notional
            );
        }

        [ObserveEvent(typeof(OrderResponseReceivedEvent))]
        private void OnOrderResponseReceivedEvent(OrderResponseReceivedEvent e)
        {
            if(e.QuoteId != _quoteId) return;
            Log.DebugFormat("RFQ response received");
            if (e.HasException)
            {
                // todo
            }
            else
            {
                _rate = e.Rate;
                _status = e.Status;
            }
            if (_status.IsEndState())
            {
                _inFlightRfqQuoteDetails = null;
                _quoteId = Guid.Empty;
            }
        }

        [ObserveEvent(typeof(AcceptQuoteEvent))]
        private void OnAcceptQuoteEvent()
        {
            Log.DebugFormat("Accepting quote");
            _status = QuoteStatus.Booking;
            _acceptQuoteDisposable.Disposable = _rfqGateway.BegingAcceptQuote(_quoteId);
        }

        [ObserveEvent(typeof(RejectQuoteEvent))]
        private void OnRejectQuoteEvent()
        {
            Log.DebugFormat("Rejecting quote");
            _status = QuoteStatus.Rejecting;
            _rejectQuoteDisposable.Disposable = _rfqGateway.BegingRejectQuote(_quoteId);
        }

        public void OnPostProcessing()
        {
            if (_inFlightRfqQuoteDetails != null)
            {
                _rfqSummary = string.Format(
                    "You BUY {0} {1} against {2} at {3}",
                    _inFlightRfqQuoteDetails.Notional,
                    _inFlightRfqQuoteDetails.CurrencyPair.Base,
                    _inFlightRfqQuoteDetails.CurrencyPair.Counter,
                    _rate.HasValue ? _rate.Value.ToString() : "[Requesting Rate]"
                );
            }
            else
            {
                _rfqSummary = null;
            }
        }

        private class InFlightRfqQuoteDetails
        {
            public Guid QuoteId { get; set; }
            public CurrencyPair CurrencyPair { get; set; }
            public decimal Notional { get; set; }
        }
    }
}