using Esp.Net.Examples.ReactiveModel.ClientApp.UI.RfqScreen;

namespace Esp.Net.Examples.ReactiveModel.ClientApp.UI.Shell
{
    public class ClientAppShellViewModel
    {
        public ClientAppShellViewModel(ClientRfqScreenViewModel clientRfqScreenViewModel)
        {
            ClientRfqScreenViewModel = clientRfqScreenViewModel;
        }

        public ClientRfqScreenViewModel ClientRfqScreenViewModel { get; private set; }
    }
}