using System;

namespace Esp.Net
{
    public interface ITerminalErrorHandler
    {
        void OnError(Exception exception); 
    }
}