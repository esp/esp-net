using System.Windows;

namespace Esp.Net.Examples.ReactiveModel
{
    public partial class App 
    {
        private readonly Bootstrapper _bootstrapper = new Bootstrapper();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            _bootstrapper.Run();
        }
    }
}
