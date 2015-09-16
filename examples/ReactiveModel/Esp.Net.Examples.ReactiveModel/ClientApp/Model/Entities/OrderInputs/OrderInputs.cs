using Esp.Net.Examples.ReactiveModel.ClientApp.Model.Events;
using Esp.Net.Examples.ReactiveModel.ClientApp.Services.Entities;
using Esp.Net.Examples.ReactiveModel.Common.Model.Entities.Fields;

namespace Esp.Net.Examples.ReactiveModel.ClientApp.Model.Entities.OrderInputs
{
    public class OrderInputs : DisposableBase
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(OrderInputs));

        private readonly SelectionField<CurrencyPair> _currencyPair;
        private readonly Field<decimal?> _notional;
        private string _orderSummary;

        public OrderInputs()
        {
            _currencyPair = new SelectionField<CurrencyPair>();
            _notional = new Field<decimal?>();
        }

        public ISelectionField<CurrencyPair> CurrencyPair
        {
            get { return _currencyPair; }
        }

        public IField<decimal?> Notional
        {
            get { return _notional; }
        }

        public string OrderSummary
        {
            get { return _orderSummary; }
        }

        [ObserveEvent(typeof(NotionalChangedEvent))]
        private void OnNotionalChangedEvent(NotionalChangedEvent e)
        {
            Log.DebugFormat("Setting notional to {0}", e.Notional);
            _notional.Value = e.Notional;
        }

        [ObserveEvent(typeof(CurrencyPairChangedEvent))]
        private void OnCurrencyPairChangedEvent(CurrencyPairChangedEvent e)
        {
            Log.DebugFormat("Setting selected currency pair to {0}", e.CurrencyPair.IsoCode);
            _currencyPair.Value = e.CurrencyPair;
        }

        [ObserveEvent(typeof(ReferenceDataReceivedEvent), ObservationStage.Committed)]
        private void OnReferenceDataReceivedEvent(OrderScreen model)
        {
            Log.DebugFormat("Applying reference data symbols");
            _currencyPair.Items.AddRange(model.CurrencyPairs);
        }

        public void OnPostProcessing(OrderScreen orderScreen)
        {
            var isEnabled = orderScreen.CurrencyPairs != null && orderScreen.CurrencyPairs.Count > 0 && !orderScreen.Rfq.Status.RfqInFlight();
            _currencyPair.IsEnabled = isEnabled;
            _notional.IsEnabled = isEnabled;
            if (_notional.HasValue && _currencyPair.HasValue)
            {
                _orderSummary = string.Format(
                    "You BUY {0} {1} against {2}",
                   _notional.Value,
                   _currencyPair.Value.Base,
                   _currencyPair.Value.Counter
                );
            }
            else
            {
                _orderSummary = "Please select both notional and currency pair above";
            }
        }
    }
}