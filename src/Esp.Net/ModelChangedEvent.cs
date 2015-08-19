namespace Esp.Net
{
    public class ModelChangedEvent<TModel>
    {
        public ModelChangedEvent(object modelId, TModel model)
        {
            Model = model;
            ModelId = modelId;
        }

        public TModel Model { get; private set; }

        public object ModelId { get; private set; }
    }
}