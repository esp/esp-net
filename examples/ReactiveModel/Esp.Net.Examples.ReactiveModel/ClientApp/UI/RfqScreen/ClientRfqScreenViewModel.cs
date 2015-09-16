using System;
using System.Reactive.Linq;
using Esp.Net.Examples.ReactiveModel.ClientApp.Model.Events;
using Esp.Net.Examples.ReactiveModel.ClientApp.Services.Entities;
using Esp.Net.Examples.ReactiveModel.Common.UI;
using Esp.Net.Examples.ReactiveModel.Common.UI.Fields;

namespace Esp.Net.Examples.ReactiveModel.ClientApp.UI.RfqScreen
{
    public class ClientRfqScreenViewModel : ViewModelBase
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(ClientRfqScreenViewModel));
        private readonly IRouter<Model.Entities.OrderScreen> _router;
        private readonly EntryMonitor _entryMonitor = new EntryMonitor();

        public ClientRfqScreenViewModel(IRouter<Model.Entities.OrderScreen> router)
        {
            _router = router;
        }

        private Guid _quoteId;
        public Guid QuoteId
        {
            get { return _quoteId; }
            private set
            {
                SetProperty(ref _quoteId, value);
            }
        }

        private string _rfqSummary;
        public string RfqSummary
        {
            get { return _rfqSummary; }
            private set
            {
                SetProperty(ref _rfqSummary, value);
            }
        }

        private string _orderSummary;
        public string OrderSummary
        {
            get { return _orderSummary; }
            private set
            {
                SetProperty(ref _orderSummary, value);
            }
        }

        private readonly FieldViewModel<decimal?> _notional = new FieldViewModel<decimal?>();
        public FieldViewModel<decimal?> Notional
        {
            get { return _notional; }
        }

        private readonly SelectionFieldViewModel<CurrencyPair> _currencyPair = new SelectionFieldViewModel<CurrencyPair>();
        public SelectionFieldViewModel<CurrencyPair> CurrencyPair
        {
            get { return _currencyPair; }
        }

        private QuoteStatus _status;
        public QuoteStatus Status
        {
            get { return _status; }
            private set
            {
                SetProperty(ref _status, value);
            }
        }

        private DelegateCommand _requestQuote;
        public DelegateCommand RequestQuote
        {
            get { return _requestQuote; }
            private set
            {
                SetProperty(ref _requestQuote, value);
            }
        }

        private bool _isRequestQuoteButtonVisible;
        public bool IsRequestQuoteButtonVisible
        {
            get { return _isRequestQuoteButtonVisible; }
            private set
            {
                SetProperty(ref _isRequestQuoteButtonVisible, value);
            }
        }

        private DelegateCommand _rejectQuoteCommand;
        public DelegateCommand RejectQuoteCommand
        {
            get { return _rejectQuoteCommand; }
            private set
            {
                SetProperty(ref _rejectQuoteCommand, value);
            }
        }

        private DelegateCommand _acceptQuoteCommand;
        public DelegateCommand AcceptQuoteCommand
        {
            get { return _acceptQuoteCommand; }
            private set
            {
                SetProperty(ref _acceptQuoteCommand, value);
            }
        }

        private bool _quotingButtonsVisible;
        public bool QuotingButtonsVisible
        {
            get { return _quotingButtonsVisible; }
            private set
            {
                SetProperty(ref _quotingButtonsVisible, value);
            }
        }

        private decimal? _rate;
        public decimal? Rate
        {
            get { return _rate; }
            private set
            {
                SetProperty(ref _rate, value);
            }
        }

        public void Start()
        {
            ObserveChanges();
            SyncViewWithModel();
        }

        private void ObserveChanges()
        {
            RequestQuote = new DelegateCommand(
                _ => _router.PublishEvent(new RequestQuoteEvent()), 
                _ => Notional.HasValue && CurrencyPair.HasValue && _status != QuoteStatus.Quoting
            );
            AcceptQuoteCommand = new DelegateCommand(
                _ => _router.PublishEvent(new AcceptQuoteEvent(_quoteId)),
                _ => _status == QuoteStatus.Quoting
            );
            RejectQuoteCommand = new DelegateCommand(
                _ => _router.PublishEvent(new RejectQuoteEvent(_quoteId)),
                _ => _status == QuoteStatus.Quoting
            );
            Notional
                .ObserveProperty(q => q.Value)
                .Where(_ => !_entryMonitor.IsBusy)
                .Subscribe(quantity => _router.PublishEvent(new NotionalChangedEvent(quantity)));
            CurrencyPair
                .ObserveProperty(q => q.Value)
                .Where(_ => !_entryMonitor.IsBusy)
                .Subscribe(symbol => _router.PublishEvent(new CurrencyPairChangedEvent(symbol)));
        }

        private void SyncViewWithModel()
        {
            AddDisposable(_router.GetModelObservable().Observe(model =>
            {
                using (_entryMonitor.Enter())
                {
                    Log.DebugFormat("Model update received. Version: {0}", model.Version);
                    QuoteId = model.Rfq.QuoteId;
                    OrderSummary = model.Inputs.OrderSummary;
                    RfqSummary = model.Rfq.RfqSummary;
                    Notional.Sync(model.Inputs.Notional);
                    CurrencyPair.Sync(model.Inputs.CurrencyPair);
                    Status = model.Rfq.Status;
                    Rate = model.Rfq.Rate;
                    RequestQuote.RaiseCanExecuteChanged();
                    AcceptQuoteCommand.RaiseCanExecuteChanged();
                    RejectQuoteCommand.RaiseCanExecuteChanged();
                    IsRequestQuoteButtonVisible = !model.Rfq.Status.RfqInFlight();
                    QuotingButtonsVisible = model.Rfq.Status == QuoteStatus.Quoting;
                }
            }));
        }
    }
}