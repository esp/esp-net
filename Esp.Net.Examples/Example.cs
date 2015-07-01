using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using Esp.Net.Concurrency;
using Esp.Net.Model;
using Esp.Net.RxBridge;
#if ESP_EXPERIMENTAL

namespace Esp.Net.Examples
{
    public class StructuredProduct
    {
        public string CurrencyPair { get; set; }
        public DateTime[] FixingDates { get; set; }
    }

    public class UserChangedCCyPairEvent
    {
        public string NewCcyPair { get; set; }
    }

    public class ReferenceDataServiceClient : IReferenceDataServiceClient
    {
        private readonly List<IObserver<DateTime[]>> _observers = new List<IObserver<DateTime[]>>();

        // hack to simulate server responding with sending dates
        public void SendDates(params DateTime[] dates)
        {
            foreach (IObserver<DateTime[]> observer in _observers)
            {
                observer.OnNext(dates);
            }
        }

        public IObservable<DateTime[]> GetFixingDates(string currencyPair)
        {
            return EspObservable.Create<DateTime[]>(o =>
            {
                _observers.Add(o); // HACK side effect : hold observers locally
                return () => { };
            });
        }
    }

    public interface IReferenceDataServiceClient
    {
        IObservable<DateTime[]> GetFixingDates(string currencyPair);
    }

    public class SerialDisposable : IDisposable
    {
        private IDisposable _disposable;

        public IDisposable Disposable
        {
            get { return _disposable; }
            set
            {
                using (_disposable) { }
                _disposable = value;
            }
        }

        public void Dispose()
        {
            using (_disposable) { }
        }
    }

    public class ReferenceDatesEventProcessor  : DisposableBase, IEventProcessor
    {
        private readonly IRouter<StructuredProduct> _router;
        private readonly IReferenceDataServiceClient _referenceDataService;
        private readonly IWorkItem<StructuredProduct> _getReferenceDatesWorkItem;
        private readonly SerialDisposable _inflightWorkItem = new SerialDisposable();

        public ReferenceDatesEventProcessor(IRouter<StructuredProduct> router, IReferenceDataServiceClient referenceDataService)
        {
            _router = router;
            _referenceDataService = referenceDataService;
            _getReferenceDatesWorkItem = _router
               .CreateWorkItemBuilder()
               .SubscribeTo(m => _referenceDataService.GetFixingDates(m.CurrencyPair), OnFixingDatesReceived)
               .CreateWorkItem();
            AddDisposable(_inflightWorkItem);
        }

        public void Start()
        {
            AddDisposable(_router
                .GetEventObservable<UserChangedCCyPairEvent>(ObservationStage.Committed)
                .Observe((model, userChangedCCyPairEvent, context) =>
                {
                    IWorkItemInstance<StructuredProduct> instance = _getReferenceDatesWorkItem.CreateInstance();
                    _inflightWorkItem.Disposable = instance;
                    instance.Run(model, ex => { });
                })
            );
        }

        public void Start2()
        {
            AddDisposable(_router
                .WorkItem()
                .OnEvent<UserChangedCCyPairEvent>((m, e, c) => { }, ObservationStage.Committed)
                .Do((m) => { })
                .SubscribeTo(m => _referenceDataService.GetFixingDates(m.CurrencyPair), OnFixingDatesReceived)
                .Do((m) => { })
                .Run()
            );
        }

        private void OnFixingDatesReceived(StructuredProduct model, DateTime[] fixingDates)
        {
            // apply dates to model
        }


    }
}
#endif