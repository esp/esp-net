using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Esp.Net.Examples.ReactiveModel.Common.UI;
using Esp.Net.Examples.ReactiveModel.TraderApp.Model.Entities;

namespace Esp.Net.Examples.ReactiveModel.TraderApp.UI.RfqScreen
{
    public class TraderRfqScreenViewModel : ViewModelBase
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(TraderRfqScreenViewModel));
        private readonly IRouter<Model.Entities.RfqScreen> _router;
        private readonly Func<RfqDetailsViewModel> _rfqDetailsViewModelFactory;
        private readonly ObservableCollection<RfqDetailsViewModel> _rfqs = new ObservableCollection<RfqDetailsViewModel>();
        private readonly Dictionary<Guid, RfqDetailsViewModel> _rfqsById = new Dictionary<Guid, RfqDetailsViewModel>();
         
        public TraderRfqScreenViewModel(IRouter<Model.Entities.RfqScreen> router, Func<RfqDetailsViewModel> rfqDetailsViewModelFactory)
        {
            _router = router;
            _rfqDetailsViewModelFactory = rfqDetailsViewModelFactory;
        }

        public ObservableCollection<RfqDetailsViewModel> Rfqs
        {
            get { return _rfqs; }
        }

        public void Start()
        {
            SyncViewWithModel();
        }

        private void SyncViewWithModel()
        {
            AddDisposable(_router.GetModelObservable().Observe(model =>
            {
                Log.DebugFormat("Model update received. Version: {0}", model.Version);
                foreach (RfqDetails rfq in model.Rfqs)
                {
                    RfqDetailsViewModel vm;
                    // we never delete RFQs on the GUI so this sync is easy
                    if (!_rfqsById.TryGetValue(rfq.QuoteId, out vm))
                    {
                        vm = _rfqDetailsViewModelFactory();
                        _rfqsById.Add(rfq.QuoteId, vm);
                        Rfqs.Insert(0, vm);
                        vm.Start(rfq);
                    }
                }
            }));
        }
    }
}