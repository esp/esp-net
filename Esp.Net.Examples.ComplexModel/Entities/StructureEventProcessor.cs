using Esp.Net.ModelRouter;

namespace Esp.Net.Examples.ComplexModel.Entities
{
    internal class StructureEventProcessor 
    {
        private StructureModel _model;
        private readonly IRouter<StructureModel> _router;

        public StructureEventProcessor(IRouter router, IReferenceData referenceData)
        {
            _model = new StructureModel(referenceData);
            router.RegisterModel(_model.Id, _model);
            _router = router.CreateModelRouter<StructureModel>(_model.Id);
        }

        public void Start()
        {
            _router.GetEventObservable<NotionalChangedEvent>().Observe((m, e) => m.SetNotional(e.Notional));
            _router.GetEventObservable<CurrencyPairChangedEvent>().Observe((m, e) => m.SetCurrencyPair(e.CurrencyPair));
        }
    }
}