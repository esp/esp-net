using Esp.Net.Examples.ReactiveModel.TraderApp.UI.RfqScreen;

namespace Esp.Net.Examples.ReactiveModel.TraderApp.UI.Shell
{
    public class TraderAppShellViewModel
    {
        public TraderAppShellViewModel(TraderRfqScreenViewModel traderRfqScreenViewModel)
        {
            TraderRfqScreenViewModel = traderRfqScreenViewModel;
        }

        public TraderRfqScreenViewModel TraderRfqScreenViewModel { get; private set; }
    }
}