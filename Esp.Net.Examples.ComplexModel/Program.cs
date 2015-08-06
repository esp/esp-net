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

        private readonly RouterScheduler _businessLogicScheduler = new RouterScheduler();

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
            _businessLogicScheduler.Schedule(BootstrapSystem);
        }

        private void BootstrapSystem()
        {
            // Typically any type creation would be done by a container. It's just done manually here for the example.

            IRouter router = new Router(_businessLogicScheduler);

            // Create some gateways that perform async operations and raise their results as events back to the router.
            // The model will own these.
            IReferenceDataGateway referenceDataTask = new ReferenceDataGateway(router, _businessLogicScheduler);
            IScheduleGenerationGateway scheduleGenerationGateway = new ScheduleGenerationGateway(router, _businessLogicScheduler);

            var modelId = Guid.NewGuid();

            // Create the model and event processor/s  
            StructureModel model = new StructureModel(modelId, referenceDataTask, scheduleGenerationGateway);
            // A single event processer is used for pre, process and post processing of events
            StructureEventProcessor eventProcessor = new StructureEventProcessor(router, modelId);
            router.RegisterModel(modelId, model, eventProcessor, eventProcessor);

            // Create a more specialised view of the model for the controller.
            // This specialised view simply extracts an immutable view of the model (StructureSnapshot).
            IModelObservable<StructureSnapshot> modelObservable = router.GetModelObservable<StructureModel>(modelId).Select(m => m.CreateSnapshot());
            ViewController controller = new ViewController(modelId, router, modelObservable);

            // Spin up the system
            controller.Start();
            eventProcessor.Start();

            // Fake up some users interactions
            _businessLogicScheduler.Schedule(TimeSpan.FromSeconds(1), () => controller.FakeCurrencyChanged());
            _businessLogicScheduler.Schedule(TimeSpan.FromSeconds(2), () => controller.FakeNotionalChanged());
            _businessLogicScheduler.Schedule(TimeSpan.FromSeconds(2), () => controller.FakeFixingFrequencyChanged());
            _businessLogicScheduler.Schedule(TimeSpan.FromSeconds(10), () => controller.FakeNotionalPerFixingChanged());
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
