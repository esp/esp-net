namespace Esp.Net
{
    public interface IPreEventProcessor<in TModel>
    {
        void Process(TModel model);
    }

    public interface IEventProcessor
    {
        void Start();
    }

    public interface IPostEventProcessor<in TModel>
    {
        void Process(TModel model);
    }
}