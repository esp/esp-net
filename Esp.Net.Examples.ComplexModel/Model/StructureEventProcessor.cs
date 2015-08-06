using System;
using Esp.Net.Examples.ComplexModel.Model.Events;

namespace Esp.Net.Examples.ComplexModel.Model
{
    internal class StructureEventProcessor : DisposableBase,  IPreEventProcessor<StructureModel>,  IPostEventProcessor<StructureModel>
    {
        private readonly IRouter _router;
        private readonly Guid _modelId;

        public StructureEventProcessor(IRouter router, Guid modelId)
        {
            _router = router;
            _modelId = modelId;
        }

        public void Start()
        {
            AddDisposable(_router.GetEventObservable<StructureModel, NotionalChangedEvent>(_modelId).Observe((m, e) => m.SetNotional(e.Notional)));
            AddDisposable(_router.GetEventObservable<StructureModel, CurrencyPairChangedEvent>(_modelId).Observe((m, e, context) => m.SetCurrencyPair(e.CurrencyPair)));
            AddDisposable(_router.GetEventObservable<StructureModel, CurrencyPairReferenceDataReceivedEvent>(_modelId).Observe((m, e) => m.ReceiveCurrencyPairReferenceData(e.RefData)));
            AddDisposable(_router.GetEventObservable<StructureModel, FixingFrequencyChangedEvent>(_modelId).Observe((m, e) => m.SetFixingFrequency(e.Frequency)));
            AddDisposable(_router.GetEventObservable<StructureModel, ScheduleResolvedEvent>(_modelId).Observe((m, e) => m.AddScheduleCoupons(e.Coupons)));
            AddDisposable(_router.GetEventObservable<StructureModel, SetNotionalPerFixingEvent>(_modelId).Observe((m, e) => m.SetNotionalPerFixing(e.NotionalPerFixing)));
        }

        void IPreEventProcessor<StructureModel>.Process(StructureModel model)
        {
            model.IncrementVersion();
        }

        void IPostEventProcessor<StructureModel>.Process(StructureModel model)
        {
            model.Validate();
        }
    }
}