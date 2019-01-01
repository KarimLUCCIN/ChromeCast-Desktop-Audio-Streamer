using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
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

        public DeviceViewModel CurrentDevice { get; set; }
        public bool HasCurrentDevice => IsInDesignMode ? true : CurrentDevice != null;
        public bool HasNoCurrentDevice => CurrentDevice == null;

        public RelayCommand<DeviceViewModel> SelectDeviceCommand { get; private set; }

        public ChromecastViewModel()
        {
            SelectDeviceCommand = new RelayCommand<DeviceViewModel>(SelectDevice);

            DevicesEnumeratorViewModel.ScanForDevicesCommand.Execute(null);
        }

        private void SelectDevice(DeviceViewModel device)
        {
            CurrentDevice = device;
        }
    }
}
