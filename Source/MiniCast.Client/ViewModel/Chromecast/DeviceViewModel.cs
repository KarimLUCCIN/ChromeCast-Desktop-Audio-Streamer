using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChromeCast.Library.Communication;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Rssdp;

namespace MiniCast.Client.ViewModel.Chromecast
{
    public class DeviceViewModel : ViewModelBase, INotifyPropertyChanged
    {
        private readonly DiscoveredSsdpDevice discoveredDevice;
        private readonly SsdpDevice device;

        public string Host => discoveredDevice.DescriptionLocation.Host;
        public SsdpDevice DeviceInfo => device;

        public DeviceState State { get; private set; }

        public bool CanPlay => !(State == DeviceState.Playing || State == DeviceState.Buffering);
        public bool CanStop => !(State == DeviceState.Closed || State == DeviceState.Disposed || State == DeviceState.ConnectError || State == DeviceState.InvalidRequest || State == DeviceState.NotConnected);
        public bool CanPause => State == DeviceState.Playing || State == DeviceState.Buffering;

        public RelayCommand PlayCommand { get; private set; }
        public RelayCommand StopCommand { get; private set; }
        public RelayCommand PauseCommand { get; private set; }

        public DeviceViewModel(DiscoveredSsdpDevice discoveredDevice, SsdpDevice device)
        {
            this.discoveredDevice = discoveredDevice ?? throw new ArgumentNullException(nameof(discoveredDevice));
            this.device = device ?? throw new ArgumentNullException(nameof(device));

            State = DeviceState.NotConnected;

            PlayCommand = new RelayCommand(Play, () => CanPlay);
            PauseCommand = new RelayCommand(Pause, () => CanPause);
            StopCommand = new RelayCommand(Stop, () => CanStop);
        }

        private void Play()
        {

        }

        private void Stop()
        {

        }

        private void Pause()
        {

        }
    }
}
