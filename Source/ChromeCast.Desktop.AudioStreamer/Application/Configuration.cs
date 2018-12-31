﻿using System;
using System.Configuration;

namespace ChromeCast.Desktop.AudioStreamer.Application
{
    public class Configuration
    {
        public void Load(Action<bool, bool, bool, int, bool, string, bool, bool> configurationCallback)
        {
            try
            {
                string useKeyboardShortCuts = ConfigurationManager.AppSettings["UseKeyboardShortCuts"];
                string showLog = ConfigurationManager.AppSettings["ShowLog"];
                string showLagControl = ConfigurationManager.AppSettings["ShowLagControl"];
                string lagControlValue = ConfigurationManager.AppSettings["LagControlValue"];
                string autoStartDevices = ConfigurationManager.AppSettings["AutoStartDevices"];
                string ipAddressesDevices = ConfigurationManager.AppSettings["IpAddressesDevices"];
                string showWindowOnStart = ConfigurationManager.AppSettings["ShowWindowOnStart"];
                string autoRestartDevices = ConfigurationManager.AppSettings["AutoRestart"];

                bool useShortCuts;
                bool boolShowLog;
                bool showLag;
                int lagValue;
                bool autoStart;
                bool showWindow;
                bool autoRestart;
                bool.TryParse(useKeyboardShortCuts, out useShortCuts);
                bool.TryParse(showLog, out boolShowLog);
                bool.TryParse(showLagControl, out showLag);
                int.TryParse(lagControlValue, out lagValue);
                bool.TryParse(autoStartDevices, out autoStart);
                bool.TryParse(showWindowOnStart, out showWindow);
                bool.TryParse(autoRestartDevices, out autoRestart);

                configurationCallback(useShortCuts, boolShowLog, showLag, lagValue, autoStart, ipAddressesDevices, showWindow, autoRestart);
            }
            catch (Exception)
            {
            }
        }
    }
}
