using System;
using Esp.Net.Examples.ComplexModel.Model.Entities;
using Esp.Net.Examples.ComplexModel.Model.Events;

namespace Esp.Net.Examples.ComplexModel.Controllers
{
    internal class ViewController : DisposableBase
    {
        private readonly Guid _modelId;
        private readonly IRouter _router;

        public ViewController(Guid modelId, IRouter router)
        {
            _modelId = modelId;
            _router = router;
        }

        public void Start()
        {
            ObserveModel();
        }

        private void ObserveModel()
        {
            AddDisposable(_router.GetModelObservable<StructureModel>(_modelId).Observe(structureModel =>
            {
                Console.WriteLine("CONTROLLER: model update: {0}", structureModel.ToString());
            }));
        }

        public void FakeCurrencyChanged()
        {
            _router.PublishEvent(_modelId, new CurrencyPairChangedEvent("EURUSD"));
        }

        public void FakeNotionalChanged()
        {
            _router.PublishEvent(_modelId, new NotionalChangedEvent(1.2354m));
        }
    }
}