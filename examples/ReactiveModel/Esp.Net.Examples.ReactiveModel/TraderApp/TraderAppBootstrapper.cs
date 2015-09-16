using System;
using Autofac;
using Esp.Net.Examples.ReactiveModel.Common;
using Esp.Net.Examples.ReactiveModel.Common.Services;
using Esp.Net.Examples.ReactiveModel.TraderApp.Model.Entities;
using Esp.Net.Examples.ReactiveModel.TraderApp.Model.Events;
using Esp.Net.Examples.ReactiveModel.TraderApp.Model.Gateways;
using Esp.Net.Examples.ReactiveModel.TraderApp.Services;
using Esp.Net.Examples.ReactiveModel.TraderApp.UI.RfqScreen;
using Esp.Net.Examples.ReactiveModel.TraderApp.UI.Shell;
using log4net;

namespace Esp.Net.Examples.ReactiveModel.TraderApp
{
    public class TraderAppBootstrapper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(TraderAppBootstrapper));

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

            // views and view models
            builder
                .RegisterType<TraderAppShellView>()
                .SingleInstance();

            builder
                .RegisterType<TraderAppShellViewModel>()
                .SingleInstance();

            builder
                .RegisterType<TraderRfqScreenViewModel>()
                .SingleInstance();
            builder
                .RegisterType<RfqDetailsViewModel>();

            // services
            builder
                .RegisterType<SchedulerService>()
                .As<ISchedulerService>()
                .SingleInstance();
            builder
                .RegisterInstance(FakeMiddleware.Instance)
                .As<IRfqService>()
                .SingleInstance();

            // router
            builder
                .RegisterType<Router>()
                .SingleInstance();

            // model
            builder
                .RegisterType<RfqServiceGateway>()
                .As<IRfqServiceGateway>()
                .SingleInstance();
            builder
                .RegisterType<RfqScreen>()
                .SingleInstance();
            builder
                .RegisterType<RfqDetails>();

            _container = builder.Build();
        }

        private void CreateAndRegisterModel()
        {
            ContainerBuilder builder = new ContainerBuilder();
            var router = _container.Resolve<Router>();

            var modelId = Guid.NewGuid();
            IRouter<RfqScreen> modelRouter = router.CreateModelRouter<RfqScreen>(modelId);
            builder.RegisterInstance(modelRouter);
            builder.Update(_container);
            var model = _container.Resolve<RfqScreen>();
            router.RegisterModel(modelId, model);
        }

        private void StartUi()
        {
            var model = _container.Resolve<RfqScreen>();
            var router = _container.Resolve<IRouter<RfqScreen>>();
            var rfqScreenViewModel = _container.Resolve<TraderRfqScreenViewModel>();
            var traderAppShellView = _container.Resolve<TraderAppShellView>();

            model.ObserveEvents();
            rfqScreenViewModel.Start();
            traderAppShellView.Show();

            router.PublishEvent(new InitialiseEvent());
        }
    }
}