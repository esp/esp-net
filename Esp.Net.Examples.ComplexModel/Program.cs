using System;
using System.Reactive.Concurrency;
using Esp.Net.Examples.ComplexModel.Controllers;
using Esp.Net.Examples.ComplexModel.Model;
using Esp.Net.Examples.ComplexModel.Model.Entities;
using Esp.Net.Examples.ComplexModel.Model.Entities.ReferenceData;

namespace Esp.Net.Examples.ComplexModel
{
    class Program
    {
        static readonly EventLoopScheduler ModelScheduler = new EventLoopScheduler();

        static void Main(string[] args)
        {
            try
            {
                var router = new Router(ThreadGuard.Default);

                var referenceDataTask = new ReferenceDataTask(router, ModelScheduler);

                var model = new StructureModel(referenceDataTask);
                router.RegisterModel(model.Id, model);

                var eventProcessor = new StructureEventProcessor(router.CreateModelRouter<StructureModel>(model.Id));
                var controller = new ViewController(model.Id, router);

                controller.Start();
                eventProcessor.Start();

                ModelScheduler.Schedule(TimeSpan.FromSeconds(1), () => controller.FakeCurrencyChanged());
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: {0}", ex.Message);
                throw;
            }

            Console.ReadLine();
        }
    }
}
