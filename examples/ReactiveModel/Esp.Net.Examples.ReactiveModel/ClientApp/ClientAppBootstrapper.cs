using System;
using Autofac;
using Esp.Net.Examples.ReactiveModel.ClientApp.Model.Entities;
using Esp.Net.Examples.ReactiveModel.ClientApp.Model.Entities.OrderInputs;
using Esp.Net.Examples.ReactiveModel.ClientApp.Model.Entities.Rfq;
using Esp.Net.Examples.ReactiveModel.ClientApp.Model.Events;
using Esp.Net.Examples.ReactiveModel.ClientApp.Model.Gateways;
using Esp.Net.Examples.ReactiveModel.ClientApp.Services;
using Esp.Net.Examples.ReactiveModel.ClientApp.UI.RfqScreen;
using Esp.Net.Examples.ReactiveModel.ClientApp.UI.Shell;
using Esp.Net.Examples.ReactiveModel.Common;
using Esp.Net.Examples.ReactiveModel.Common.Services;
using log4net;

namespace Esp.Net.Examples.ReactiveModel.ClientApp
{
    public class ClientAppBootstrapper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ClientAppBootstrapper));

        private IContainer _container;

        public void Run()
        {
            Log.Debug("Running");
            ConfigureContainer();
            CreateAndRegisterModel();
            StartUi();
        }

        private void ConfigureContainer()
        {
            ContainerBuilder builder = new ContainerBuilder();

            // For this example we just assume there is only ever 1 request for quote (RFQ) screen. 
            // In reality you'd configure the below components using .InstancePerLifetimeScope()
            // as you'd likely have many 'tiles' that can be used to place RFQs simultaneously. 

            // views and view models
            builder
                .RegisterType<ClientAppShellView>()
                .SingleInstance();

            builder
                .RegisterType<ClientAppShellViewModel>()
                .SingleInstance();

            builder
                .RegisterType<ClientRfqScreenViewModel>()
                .SingleInstance();

            // services
            builder
                .RegisterType<SchedulerService>()
                .As<ISchedulerService>()
                .SingleInstance();
            builder
                .RegisterInstance(FakeMiddleware.Instance)
                .As<IRfqServiceClient>()
                .SingleInstance();
            builder
                .RegisterInstance(FakeMiddleware.Instance)
                .As<IReferenceDataServiceClient>()
                .SingleInstance();

            // router
            builder
                .RegisterType<Router>()
                .SingleInstance();

            // model
            builder
                .RegisterType<ReferenceDataGateway>()
                .As<IReferenceDataGateway>()
                .SingleInstance();
            builder
                .RegisterType<RequestForQuoteGateway>()
                .As<IRequestForQuoteGateway>()
                .SingleInstance();
            builder
                .RegisterType<OrderScreen>()
                .SingleInstance();
            builder
                .RegisterType<OrderInputs>()
                .SingleInstance();
            builder
                .RegisterType<Rfq>()
                .SingleInstance();

            _container = builder.Build();
        }

        private void CreateAndRegisterModel()
        {
            ContainerBuilder builder = new ContainerBuilder();
            var router = _container.Resolve<Router>();

            var modelId = Guid.NewGuid();
            IRouter<OrderScreen> modelRouter = router.CreateModelRouter<OrderScreen>(modelId);
            builder.RegisterInstance(modelRouter);
            builder.Update(_container);
            var model = _container.Resolve<OrderScreen>();
            router.AddModel(modelId, model);
        }

        private void StartUi()
        {
            var model = _container.Resolve<OrderScreen>();
            var router = _container.Resolve<IRouter<OrderScreen>>();
            var orderScreenViewModel = _container.Resolve<ClientRfqScreenViewModel>();
            var clientAppShellView = _container.Resolve<ClientAppShellView>();

            model.ObserveEvents();
            orderScreenViewModel.Start();
            clientAppShellView.Show();

            // Fire a single InitialiseEvent to get the model ticking over.
            // This will result in it getting a chance to initialize and will 
            // result in any model observers (i.e. the ClientRfqScreenViewModel) 
            // receiving the first model update.
            router.PublishEvent(new InitialiseEvent());
        }
    }
}
