using System;

namespace ChromeCast.Desktop.AudioStreamer.Application
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