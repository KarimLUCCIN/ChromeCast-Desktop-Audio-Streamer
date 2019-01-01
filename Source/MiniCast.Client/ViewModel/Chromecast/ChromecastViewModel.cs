using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniCast.Client.ViewModel.Chromecast
{
    public class ChromecastViewModel : ViewModelBase, INotifyPropertyChanged
    {
        public DevicesEnumeratorViewModel DevicesEnumeratorViewModel { get; } = new DevicesEnumeratorViewModel();

        public ChromecastViewModel()
        {
            DevicesEnumeratorViewModel.ScanForDevicesCommand.Execute(null);
        }
    }
}
