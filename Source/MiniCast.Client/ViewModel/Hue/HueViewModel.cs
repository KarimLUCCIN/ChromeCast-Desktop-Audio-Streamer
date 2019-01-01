using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniCast.Client.ViewModel.Hue
{
    public class HueViewModel : ViewModelBase, INotifyPropertyChanged
    {
        public HueDevicesEnumeratorViewModel DevicesEnumeratorViewModel { get; } = new HueDevicesEnumeratorViewModel();

        public HueViewModel()
        {
            DevicesEnumeratorViewModel.ScanForDevicesCommand.Execute(null);
        }
    }
}
