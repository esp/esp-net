using System;

namespace Esp.Net
{
    public class ModelChangedEvent<TModel>
    {
        public ModelChangedEvent(Guid modelId, TModel model)
        {
            ModelId = modelId;
            Model = model;
        }

        public Guid ModelId { get; private set; }

        public TModel Model { get; private set; }
    }
}