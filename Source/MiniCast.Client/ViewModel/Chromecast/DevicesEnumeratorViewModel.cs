using ChromeCast.Library.Discover;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Rssdp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniCast.Client.ViewModel.Chromecast
{
    public class DevicesEnumeratorViewModel : ViewModelBase, INotifyPropertyChanged
    {
        private DiscoverDevices discoverDevices = new DiscoverDevices(new DiscoverServiceSSDP());

        public bool IsBusy { get; set; }
        public bool IsReady => !IsBusy;

        public RelayCommand ScanForDevicesCommand { get; private set; }

        public ObservableCollection<DeviceViewModel> KnownDevices { get; } = new ObservableCollection<DeviceViewModel>();

        public DevicesEnumeratorViewModel()
        {
            ScanForDevicesCommand = new RelayCommand(ScanForDevices, () => !IsBusy);
        }

        public override void Cleanup()
        {
            foreach(var device in KnownDevices)
            {
                device.Cleanup();
            }

            base.Cleanup();
        }

        private void ScanForDevices()
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;
            ScanForDevicesCommand.RaiseCanExecuteChanged();
            try
            {
                discoverDevices.BeginDiscover(((DiscoveredSsdpDevice discoveredDevice, SsdpDevice device) deviceInfo) =>
                {
                    var existing = KnownDevices.FirstOrDefault(d => d.Host == deviceInfo.discoveredDevice.DescriptionLocation.Host);
                    if (existing == null)
                    {
                        KnownDevices.Add(new DeviceViewModel(deviceInfo.discoveredDevice, deviceInfo.device));
                    }
                });
            }
            finally
            {
                IsBusy = false;
                ScanForDevicesCommand.RaiseCanExecuteChanged();
            }
        }
    }
}
