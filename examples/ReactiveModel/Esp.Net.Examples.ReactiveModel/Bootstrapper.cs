using Esp.Net.Examples.ReactiveModel.ClientApp;
using Esp.Net.Examples.ReactiveModel.TraderApp;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;

namespace Esp.Net.Examples.ReactiveModel
{
    public class Bootstrapper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Bootstrapper));

        private ClientAppBootstrapper _clientAppBootstrapper;
        private TraderAppBootstrapper _traderAppBootstrapper;

        public void Run()
        {
            ConfigureLogging();
            Log.Debug("Running");

            _clientAppBootstrapper = new ClientAppBootstrapper();
            _clientAppBootstrapper.Run();
            _traderAppBootstrapper = new TraderAppBootstrapper();
            _traderAppBootstrapper.Run();
        }

        private void ConfigureLogging()
        {
            // Fun tip: if you change the startup type of a WPF project to console you'll see 
            // both the app windows and a console displaying the log output.

            var appender = new ColoredConsoleAppender
            {
                Threshold = Level.All,
                Layout = new PatternLayout(
                    "[%logger{1}] - %message%newline"
                ),
            };
            appender.AddMapping(new ColoredConsoleAppender.LevelColors
            {
                Level = Level.Debug,
                ForeColor = ColoredConsoleAppender.Colors.White
            });
            appender.AddMapping(new ColoredConsoleAppender.LevelColors
            {
                Level = Level.Info,
                ForeColor = ColoredConsoleAppender.Colors.Green
            });
            appender.AddMapping(new ColoredConsoleAppender.LevelColors
            {
                Level = Level.Warn,
                ForeColor = ColoredConsoleAppender.Colors.Yellow
            });
            appender.AddMapping(new ColoredConsoleAppender.LevelColors
            {
                Level = Level.Error,
                ForeColor = ColoredConsoleAppender.Colors.Red
            });
            appender.AddMapping(new ColoredConsoleAppender.LevelColors
            {
                Level = Level.Fatal,
                ForeColor = ColoredConsoleAppender.Colors.Red | ColoredConsoleAppender.Colors.HighIntensity,
                BackColor = ColoredConsoleAppender.Colors.Red
            });
            appender.ActivateOptions();
            BasicConfigurator.Configure(appender);
        }
    }
}
