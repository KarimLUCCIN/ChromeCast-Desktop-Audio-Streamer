using System;

namespace ChromeCast.Library.Application
{
    public class Logger
    {
        private Action<string> logCallback;

        public void Log(string message)
        {
            logCallback?.Invoke(message);
        }

        public void SetCallback(Action<string> logCallbackIn)
        {
            logCallback = logCallbackIn;
        }
    }
}