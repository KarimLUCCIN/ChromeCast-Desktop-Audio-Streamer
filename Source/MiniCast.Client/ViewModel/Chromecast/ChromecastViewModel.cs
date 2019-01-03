using ChromeCast.Library.Classes;
using ChromeCast.Library.Streaming;
using CSCore;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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

        public IPEndPoint ListeningEndpoint { get; private set; }

        public ChromecastViewModel()
        {
            SelectDeviceCommand = new RelayCommand<DeviceViewModel>(SelectDevice);

            DevicesEnumeratorViewModel.ScanForDevicesCommand.Execute(null);

            ViewModelLocator.Instance.LoopbackRecorder.RecordingDataAvailable += OnRecordingDataAvailable;
        }

        public override void Cleanup()
        {
            DevicesEnumeratorViewModel.Cleanup();
            base.Cleanup();
        }

        private async void OnRecordingDataAvailable(ArraySegment<byte> dataToSend, WaveFormat format)
        {
            var dataSize = dataToSend.Count;
            var tempData = ArrayPool<byte>.Shared.Rent(dataSize);
            Buffer.BlockCopy(dataToSend.Array, dataToSend.Offset, tempData, 0, dataSize);
            var tempDataView = new ArraySegment<byte>(tempData, 0, dataSize);

            foreach (var device in DevicesEnumeratorViewModel.KnownDevices)
            {
                if (device.IsConnected)
                {
                    await device.SendRecordingDataAsync(tempDataView, format);
                }
            }

            ArrayPool<byte>.Shared.Return(tempData);
        }

        private void SelectDevice(DeviceViewModel device)
        {
            CurrentDevice = device;
        }
    }
}
