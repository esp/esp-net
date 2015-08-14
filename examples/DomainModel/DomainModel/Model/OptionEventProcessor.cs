using System;
using Esp.Net.Examples.ComplexModel.Model.Events;

namespace Esp.Net.Examples.ComplexModel.Model
{
    internal class OptionEventProcessor : DisposableBase,  IPreEventProcessor<Option>,  IPostEventProcessor<Option>
    {
        private readonly IRouter _router;
        private readonly Guid _modelId;

        public OptionEventProcessor(IRouter router, Guid modelId)
        {
            _router = router;
            _modelId = modelId;
        }

        public void Start()
        {
            AddDisposable(_router.GetEventObservable<Option, NotionalChangedEvent>(_modelId).Observe((m, e) => m.SetNotional(e.Notional)));
            AddDisposable(_router.GetEventObservable<Option, CurrencyPairChangedEvent>(_modelId).Observe((m, e, context) => m.SetCurrencyPair(e.CurrencyPair)));
            AddDisposable(_router.GetEventObservable<Option, CurrencyPairReferenceDataReceivedEvent>(_modelId).Observe((m, e) => m.ReceiveCurrencyPairReferenceData(e.RefData)));
            AddDisposable(_router.GetEventObservable<Option, FixingFrequencyChangedEvent>(_modelId).Observe((m, e) => m.SetFixingFrequency(e.Frequency)));
            AddDisposable(_router.GetEventObservable<Option, ScheduleResolvedEvent>(_modelId).Observe((m, e) => m.AddScheduleCoupons(e.Coupons)));
            AddDisposable(_router.GetEventObservable<Option, SetNotionalPerFixingEvent>(_modelId).Observe((m, e) => m.SetNotionalPerFixing(e.NotionalPerFixing)));
        }

        void IPreEventProcessor<Option>.Process(Option model)
        {
            model.IncrementVersion();
        }

        void IPostEventProcessor<Option>.Process(Option model)
        {
            model.Validate();
        }
    }
}