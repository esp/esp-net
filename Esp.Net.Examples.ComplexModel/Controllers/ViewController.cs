using System;
using Esp.Net.Examples.ComplexModel.Model.Events;
using Esp.Net.Examples.ComplexModel.Model.Schedule;
using Esp.Net.Examples.ComplexModel.Model.Snapshot;
using Esp.Net.Reactive;

namespace Esp.Net.Examples.ComplexModel.Controllers
{
    internal class ViewController : DisposableBase
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(ViewController));

        private readonly Guid _modelId;
        private readonly IEventPublisher _eventPublisher;
        private readonly IModelObservable<StructureSnapshot> _modelObservable;

        public ViewController(Guid modelId, IEventPublisher eventPublisher, IModelObservable<StructureSnapshot> modelObservable)
        {
            _modelId = modelId;
            _eventPublisher = eventPublisher;
            _modelObservable = modelObservable;
        }

        public void Start()
        {
            SyncViewWithModel();
        }

        private void SyncViewWithModel()
        {
            AddDisposable(_modelObservable.Observe(structureSnapshot =>
            {
                // sync update here 
                Log.DebugFormat("Model update received: {0}", structureSnapshot);
            }));
        }

        public void FakeCurrencyChanged()
        {
            // this method would be called by the view
            _eventPublisher.PublishEvent(_modelId, new CurrencyPairChangedEvent("EURUSD"));
        }

        public void FakeNotionalChanged()
        {
            // this method would be called by the view
            _eventPublisher.PublishEvent(_modelId, new NotionalChangedEvent(1.2354m));
        }

        public void FakeFixingFrequencyChanged()
        {
            // this method would be called by the view
            _eventPublisher.PublishEvent(_modelId, new FixingFrequencyChangedEvent(FixingFrequency.Monthly));
        }
    }
}