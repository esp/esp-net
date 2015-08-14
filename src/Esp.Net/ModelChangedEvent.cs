using System;

namespace Esp.Net
{
    public class ModelChangedEvent<TModel>
    {
        public ModelChangedEvent(Guid modelId, TModel model)
        {
            Model = model;
            ModelId = modelId;
        }

        public TModel Model { get; private set; }

        public Guid ModelId { get; private set; }
    }
}