using System;

namespace Esp.Net
{
    public static class Guard
    {
        public static void Requires<TException>(bool check, string format, params object[] args)
            where TException : Exception
        {
            if (!check)
            {
                var errorMessage = string.Format(format, args);
                var exception = (TException)Activator.CreateInstance(typeof(TException), errorMessage);
                throw exception;
            }
        }
    }
}