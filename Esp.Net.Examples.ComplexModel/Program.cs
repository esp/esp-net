using System;
using System.Reactive.Concurrency;
using Esp.Net.Examples.ComplexModel.Controllers;
using Esp.Net.Examples.ComplexModel.Model;
using Esp.Net.Examples.ComplexModel.Model.ReferenceData;
using Esp.Net.Examples.ComplexModel.Model.Schedule;
using Esp.Net.Examples.ComplexModel.Model.Snapshot;
using Esp.Net.Reactive;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;

namespace Esp.Net.Examples.ComplexModel
{
    class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Program));

        private readonly RouterScheduler _scheduler = new RouterScheduler();

        static void Main(string[] args)
        {
            try
            {
                new Program().Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: {0}", ex.Message);
                throw;
            }

            Console.ReadLine();
        }

        private void Run()
        {
            ConfigureLogging();
            Log.Debug("Running");
            _scheduler.Schedule(BootstrapSystem);
        }

        private void BootstrapSystem()
        {
            // typically any type creation would be done by a container. It's just done manually here for the example.

            IRouter router = new Router(_scheduler);

            IReferenceDataGateway referenceDataTask = new ReferenceDataGateway(router, _scheduler);
            IScheduleGenerationGateway scheduleGenerationGateway = new ScheduleGenerationGateway(router, _scheduler);

            var modelId = Guid.NewGuid();

            StructureModel model = new StructureModel(modelId, referenceDataTask, scheduleGenerationGateway);
            router.RegisterModel(modelId, model, new StructurePreEventProcessor(), new StructurePostEventProcessor());

            StructureEventProcessor eventProcessor = new StructureEventProcessor(router.CreateModelRouter<StructureModel>(modelId));

            IModelObservable<StructureSnapshot> modelObservable = router.GetModelObservable<StructureModel>(modelId).Select(m => m.CreateSnapshot());

            ViewController controller = new ViewController(modelId, router, modelObservable);

            controller.Start();
            eventProcessor.Start();

            // fake up some users interactions
            _scheduler.Schedule(TimeSpan.FromSeconds(1), () => controller.FakeCurrencyChanged());
            _scheduler.Schedule(TimeSpan.FromSeconds(2), () => controller.FakeNotionalChanged());
            _scheduler.Schedule(TimeSpan.FromSeconds(2), () => controller.FakeFixingFrequencyChanged());
        }

        private void ConfigureLogging()
        {
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
