using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using Rssdp;

namespace MiniCast.Client.ViewModel.Chromecast
{
    public class DeviceViewModel : ViewModelBase, INotifyPropertyChanged
    {
        private readonly DiscoveredSsdpDevice discoveredDevice;
        private readonly SsdpDevice device;

        public string Host => discoveredDevice.DescriptionLocation.Host;
        public SsdpDevice DeviceInfo => device;

        public DeviceViewModel(DiscoveredSsdpDevice discoveredDevice, SsdpDevice device)
        {
            this.discoveredDevice = discoveredDevice ?? throw new ArgumentNullException(nameof(discoveredDevice));
            this.device = device ?? throw new ArgumentNullException(nameof(device));
        }
    }
}
