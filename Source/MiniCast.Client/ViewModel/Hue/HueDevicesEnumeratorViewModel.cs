using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using MiniCast.Hue;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniCast.Client.ViewModel.Hue
{
    public class HueDevicesEnumeratorViewModel : ViewModelBase, INotifyPropertyChanged
    {
        public bool IsBusy { get; set; }
        public bool IsReady => !IsBusy;

        public RelayCommand ScanForDevicesCommand { get; private set; }

        public ObservableCollection<HueEndpointViewModel> KnownDevices { get; } = new ObservableCollection<HueEndpointViewModel>();

        public HueDevicesEnumeratorViewModel()
        {
            ScanForDevicesCommand = new RelayCommand(ScanForDevices, () => !IsBusy);
        }

        private async void ScanForDevices()
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;
            try
            {
                var devices = await HueEndpointsEnumerator.EnumerateDevices();
                foreach(var deviceInfo in devices)
                {
                    var existing = KnownDevices.FirstOrDefault(d => d.Id == deviceInfo.Id);
                    if (existing == null)
                    {
                        KnownDevices.Add(new HueEndpointViewModel(deviceInfo));
                    }
                }
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
