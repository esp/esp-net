using Esp.Net.Examples.ComplexModel.Model.Events;
using Esp.Net.ModelRouter;
using log4net;

namespace Esp.Net.Examples.ComplexModel.Model
{
    internal class StructurePreEventProcessor : IPreEventProcessor<StructureModel>
    {
        public void Process(StructureModel model)
        {
            model.IncrementVersion();
        }
    }

    internal class StructureEventProcessor : DisposableBase
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(StructureEventProcessor));

        private readonly IRouter<StructureModel> _router;

        public StructureEventProcessor(IRouter<StructureModel> router)
        {
            _router = router;
        }

        public void Start()
        {
            AddDisposable(_router.GetEventObservable<NotionalChangedEvent>().Observe((m, e) => m.SetNotional(e.Notional)));
            AddDisposable(_router.GetEventObservable<CurrencyPairChangedEvent>().Observe((m, e, context) => m.SetCurrencyPair(e.CurrencyPair)));
            AddDisposable(_router.GetEventObservable<CurrencyPairReferenceDataReceivedEvent>().Observe((m, e) => m.ReceiveCurrencyPairReferenceData(e.RefData)));
            AddDisposable(_router.GetEventObservable<FixingFrequencyChangedEvent>().Observe((m, e) => m.SetFixingFrequency(e.Frequency)));
            AddDisposable(_router.GetEventObservable<ScheduleResolvedEvent>().Observe((m, e) => m.AddScheduleCoupons(e.Coupons)));
        }
    }

    internal class StructurePostEventProcessor : IPostEventProcessor<StructureModel>
    {
        public void Process(StructureModel model)
        {
            model.Validate();
        }
    }
}