using System;

namespace Esp.Net
{
    public abstract class ModelChangedEvent
    {
        protected ModelChangedEvent(Guid modelId)
        {
            ModelId = modelId;
        }

        public Guid ModelId { get; private set; }

    }

    public class ModelChangedEvent<TModel> : ModelChangedEvent
    {
        public ModelChangedEvent(Guid modelId, TModel model) : base(modelId)
        {
            Model = model;
        }

        public TModel Model { get; private set; }
    }
}