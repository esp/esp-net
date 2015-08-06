namespace Esp.Net
{
    public interface IPreEventProcessor<in TModel>
    {
        void Process(TModel model);
    }
}