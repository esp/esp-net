using System;

namespace Esp.Net
{
    public interface ITerminalErrorHandler
    {
        void OnError(Exception exception); 
    }

    public class BubblingTerminalErrorHandler : ITerminalErrorHandler
    {
        public void OnError(Exception exception)
        {
            throw new NotImplementedException();
        }
    }
}