namespace Esp.Net.Examples.ReactiveModel.ClientApp.UI.Shell
{
    public partial class ClientAppShellView 
    {
        public ClientAppShellView(ClientAppShellViewModel viewModel)
        {
            InitializeComponent();
            DataContext= viewModel;
        }
    }
}
