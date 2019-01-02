using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ChromeCast.Library.Communication;
using ChromeCast.Library.Communication.Classes;
using ChromeCast.Library.Networking;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using NAudio.Wave;
using Rssdp;

namespace MiniCast.Client.ViewModel.Chromecast
{
    public class DeviceViewModel : ViewModelBase, INotifyPropertyChanged
    {
        private readonly DiscoveredSsdpDevice discoveredDevice;
        private readonly SsdpDevice device;

        private EndpointConnection deviceConnection;
        private EndpointCommunication deviceCommunication;

        public string Host => discoveredDevice.DescriptionLocation.Host;
        public SsdpDevice DeviceInfo => device;

        public DeviceState State { get; private set; }

        public bool CanPlay { get; private set; }
        public bool CanStop { get; private set; }
        public bool CanPause { get; private set; }

        public bool IsConnected => deviceConnection.ConnectionState == DeviceConnectionState.Connected;

        public RelayCommand PlayCommand { get; private set; }
        public RelayCommand StopCommand { get; private set; }
        public RelayCommand PauseCommand { get; private set; }

        public DeviceViewModel(DiscoveredSsdpDevice discoveredDevice, SsdpDevice device)
        {
            this.discoveredDevice = discoveredDevice ?? throw new ArgumentNullException(nameof(discoveredDevice));
            this.device = device ?? throw new ArgumentNullException(nameof(device));
            State = DeviceState.NotConnected;

            PlayCommand = new RelayCommand(PlayAsync, () => CanPlay);
            PauseCommand = new RelayCommand(PauseAsync, () => CanPause);
            StopCommand = new RelayCommand(StopAsync, () => CanStop);

            UpdateCommandStatus();

            deviceConnection = new EndpointConnection(Host);
            deviceCommunication = new EndpointCommunication(deviceConnection);

            var syncContext = SynchronizationContext.Current;
            deviceCommunication.StateChanged += (EndpointCommunication com, DeviceState newState) =>
            {
                syncContext.Post((_) => {
                    State = newState;

                    UpdateCommandStatus();
                }, null);
            };
        }

        public override void Cleanup()
        {
            deviceConnection.Dispose();

            base.Cleanup();
        }

        public async Task SendRecordingDataAsync(ArraySegment<byte> dataToSend, WaveFormat format)
        {
            await deviceConnection.SendRecordingDataAsync(dataToSend, format);
        }

        public async Task ConnectRecordingDataAsync(Socket socket)
        {
            if (socket == null)
            {
                throw new ArgumentNullException(nameof(socket));
            }

            await deviceConnection.ConnectRecordingDataAsync(socket);
        }

        private void UpdateCommandStatus()
        {
            switch (State)
            {
                default:
                case DeviceState.NotConnected:
                    CanPlay = true; CanPause = false; CanStop = false;
                    break;
                case DeviceState.Idle:
                    CanPlay = true; CanPause = false; CanStop = false;
                    break;
                case DeviceState.Disposed:
                    CanPlay = true; CanPause = false; CanStop = false;
                    break;
                case DeviceState.LaunchingApplication:
                    CanPlay = false; CanPause = false; CanStop = false;
                    break;
                case DeviceState.LaunchedApplication:
                    CanPlay = true; CanPause = false; CanStop = false;
                    break;
                case DeviceState.LoadingMedia:
                    CanPlay = false; CanPause = false; CanStop = true;
                    break;
                case DeviceState.Buffering:
                    CanPlay = false; CanPause = false; CanStop = true;
                    break;
                case DeviceState.Playing:
                    CanPlay = false; CanPause = true; CanStop = true;
                    break;
                case DeviceState.Paused:
                    CanPlay = true; CanPause = false; CanStop = true;
                    break;
                case DeviceState.ConnectError:
                    CanPlay = true; CanPause = false; CanStop = false;
                    break;
                case DeviceState.LoadFailed:
                    CanPlay = true; CanPause = false; CanStop = false;
                    break;
                case DeviceState.LoadCancelled:
                    CanPlay = true; CanPause = false; CanStop = false;
                    break;
                case DeviceState.InvalidRequest:
                    CanPlay = true; CanPause = false; CanStop = false;
                    break;
                case DeviceState.Closed:
                    CanPlay = true; CanPause = false; CanStop = false;
                    break;
            }

            Debug.WriteLine($"{CanPlay} {CanPause} {CanStop}");

            PlayCommand.RaiseCanExecuteChanged();
            StopCommand.RaiseCanExecuteChanged();
            PauseCommand.RaiseCanExecuteChanged();
        }

        private async void PlayAsync()
        {
            switch (State)
            {
                case DeviceState.Disposed:
                case DeviceState.NotConnected:
                case DeviceState.ConnectError:
                case DeviceState.LoadFailed:
                case DeviceState.LoadCancelled:
                case DeviceState.InvalidRequest:
                case DeviceState.Closed:
                default:
                    await deviceCommunication.LaunchAndLoadMediaAsync();
                    break;
                case DeviceState.Idle:
                case DeviceState.LaunchedApplication:
                case DeviceState.Paused:
                    await deviceCommunication.LoadMediaAsync();
                    break;
                case DeviceState.LaunchingApplication:
                case DeviceState.LoadingMedia:
                case DeviceState.Buffering:
                case DeviceState.Playing:
                    Debug.WriteLine("Play command dropped");
                    break;
            }

            UpdateCommandStatus();
        }

        private async void StopAsync()
        {
            await deviceCommunication.StopAsync();

            UpdateCommandStatus();
        }

        private async void PauseAsync()
        {
            await deviceCommunication.PauseMediaAsync();

            UpdateCommandStatus();
        }
    }
}
