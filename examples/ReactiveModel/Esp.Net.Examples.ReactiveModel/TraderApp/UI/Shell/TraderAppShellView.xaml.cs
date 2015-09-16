namespace Esp.Net.Examples.ReactiveModel.TraderApp.UI.Shell
{
    public partial class TraderAppShellView
    {
        public TraderAppShellView(TraderAppShellViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
