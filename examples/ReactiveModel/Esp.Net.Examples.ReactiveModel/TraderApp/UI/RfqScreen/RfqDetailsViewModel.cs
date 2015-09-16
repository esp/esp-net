using System;
using System.Reactive.Linq;
using Esp.Net.Examples.ReactiveModel.Common.UI;
using Esp.Net.Examples.ReactiveModel.Common.UI.Fields;
using Esp.Net.Examples.ReactiveModel.TraderApp.Model.Entities;
using Esp.Net.Examples.ReactiveModel.TraderApp.Model.Events;
using Esp.Net.Examples.ReactiveModel.TraderApp.Services.Entities;

namespace Esp.Net.Examples.ReactiveModel.TraderApp.UI.RfqScreen
{
    public class RfqDetailsViewModel : ViewModelBase
    {
        private readonly IRouter<Model.Entities.RfqScreen> _router;
        private readonly EntryMonitor _entryMonitor = new EntryMonitor();

        public RfqDetailsViewModel(IRouter<Model.Entities.RfqScreen> router)
        {
            _router = router;
        }

        private Guid _quoteId;
        public Guid QuoteId
        {
            get { return _quoteId; }
        }

        private QuoteStatus _quoteStatus;
        public QuoteStatus Status
        {
            get { return _quoteStatus; }
            private set
            {
                SetProperty(ref _quoteStatus, value);
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

        private readonly FieldViewModel<decimal?> _rate = new FieldViewModel<decimal?>();
        public FieldViewModel<decimal?> Rate
        {
            get { return _rate; }
        }

        private DelegateCommand _sendQuoteCommand;
        public DelegateCommand SendQuoteCommand
        {
            get { return _sendQuoteCommand; }
            private set
            {
                SetProperty(ref _sendQuoteCommand, value);
            }
        }

        private DelegateCommand _rejectQuote;
        public DelegateCommand RejectQuoteCommand
        {
            get { return _rejectQuote; }
            private set
            {
                SetProperty(ref _rejectQuote, value);
            }
        }

        private bool _formEnabled;
        public bool FormEnabled
        {
            get { return _formEnabled; }
            private set
            {
                SetProperty(ref _formEnabled, value);
            }
        }

        public void Start(RfqDetails rfqDetails)
        {
            _quoteId = rfqDetails.QuoteId;
            ObserveChanges();
            SyncViewWithModel();
            Update(rfqDetails);
        }

        private void ObserveChanges()
        {
            SendQuoteCommand = new DelegateCommand(
                _ => _router.PublishEvent(new TraderSendQuoteEvent(QuoteId)),
                _ => Rate.HasValue && _quoteStatus == QuoteStatus.New
            );
            RejectQuoteCommand = new DelegateCommand(
                _ => _router.PublishEvent(new TraderRejectQuoteEvent(QuoteId)),
                _ => _quoteStatus == QuoteStatus.New
            );
            Rate
                .ObserveProperty(q => q.Value)
                .Where(_ => !_entryMonitor.IsBusy)
                .Subscribe(quantity => _router.PublishEvent(new RfqRateChangedEvent(Rate.Value, QuoteId)));
        }

        public void SyncViewWithModel()
        {
            AddDisposable(_router.GetModelObservable().Observe(model =>
            {
                RfqDetails rfqDetails = model[QuoteId];
                Update(rfqDetails);
            }));
        }

        private void Update(RfqDetails rfqDetails)
        {
            using (_entryMonitor.Enter())
            {
                Status = rfqDetails.QuoteStatus;
                RfqSummary = rfqDetails.RfqSummary;
                Rate.Sync(rfqDetails.Rate);
                SendQuoteCommand.RaiseCanExecuteChanged();
                RejectQuoteCommand.RaiseCanExecuteChanged();
                FormEnabled = Status == QuoteStatus.New;
            }
        }
    }
}