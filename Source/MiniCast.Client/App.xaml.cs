using Acr.Settings;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace MiniCast.Client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            Acr.Settings.CrossSettings.Current = new AppConfigSettingsImpl("MiniCast.Config");
        }
    }
}
