using System;

namespace Esp.Net
{
    public class Router<TModel> : IRouter<TModel> where TModel : class 
    {
        private readonly IRouter<TModel> _modelRouter;

        public Router(TModel model)
            : this(model, new CurrentThreadDispatcher())
        {
        }

        public Router(TModel model, IRouterDispatcher routerDispatcher)
        {
            var router = new Router(routerDispatcher);
            var id = Guid.NewGuid();
            router.RegisterModel(id, model);
            _modelRouter = router.CreateModelRouter<TModel>(id);
        }

        public IModelObservable<TModel> GetModelObservable()
        {
            return _modelRouter.GetModelObservable();
        }

        public IEventObservable<TModel, TEvent, IEventContext> GetEventObservable<TEvent>(ObservationStage observationStage = ObservationStage.Normal)
        {
            return _modelRouter.GetEventObservable<TEvent>(observationStage);
        }

        public IEventObservable<TModel, TBaseEvent, IEventContext> GetEventObservable<TBaseEvent>(Type eventType, ObservationStage observationStage = ObservationStage.Normal)
        {
            return _modelRouter.GetEventObservable<TBaseEvent>(eventType, observationStage);
        }

        public void PublishEvent<TEvent>(TEvent @event)
        {
            _modelRouter.PublishEvent(@event);
        }

        public void PublishEvent(object @event)
        {
            _modelRouter.PublishEvent(@event);
        }

        public void ExecuteEvent<TEvent>(TEvent @event)
        {
            _modelRouter.ExecuteEvent(@event);
        }

        public void ExecuteEvent(object @event)
        {
            _modelRouter.ExecuteEvent(@event);
        }

        public void BroadcastEvent<TEvent>(TEvent @event)
        {
            _modelRouter.BroadcastEvent(@event);
        }

        public void BroadcastEvent(object @event)
        {
            _modelRouter.BroadcastEvent(@event);
        }
    }
}