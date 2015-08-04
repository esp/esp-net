using Esp.Net.Examples.ComplexModel.Model.Entities;
using Esp.Net.Examples.ComplexModel.Model.Events;
using Esp.Net.ModelRouter;

namespace Esp.Net.Examples.ComplexModel.Model
{
    internal class StructureEventProcessor : DisposableBase
    {
        private readonly IRouter<StructureModel> _router;

        public StructureEventProcessor(IRouter<StructureModel> router)
        {
            _router = router;
        }

        public void Start()
        {
            AddDisposable(_router.GetEventObservable<NotionalChangedEvent>().Observe((m, e) => m.SetNotional(e.Notional)));
            AddDisposable(_router.GetEventObservable<CurrencyPairChangedEvent>().Observe((m, e) => m.SetCurrencyPair(e.CurrencyPair)));
            AddDisposable(_router.GetEventObservable<CurrencyPairReferenceDataReceivedEvent>().Observe((m, e) => m.SetCurrencyPairReferenceData(e.RefData)));
        }
    }
}