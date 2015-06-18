namespace Esp.Net
{
    public interface IRouterScheudler
    {
        bool Checkaccess();
    }

    public class RouterScheudler : IRouterScheudler
    {
        public static IRouterScheudler Default { get; private set; }

        static RouterScheudler()
        {
            Default  = new RouterScheudler();
        }

        private RouterScheudler()
        {
        }
        
        public bool Checkaccess()
        {
            return true;
        }
    }
}