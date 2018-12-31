using System;
using System.Net.Sockets;
using System.Windows.Forms;
using Rssdp;
using NAudio.Wave;
using ChromeCast.Desktop.AudioStreamer.UserControls;
using ChromeCast.Desktop.AudioStreamer.ProtocolBuffer;
using ChromeCast.Library.Communication;
using ChromeCast.Library.Streaming;
using ChromeCast.Library.Communication.Classes;

namespace ChromeCast.Desktop.AudioStreamer.Application
{
    public class Device
    {
        private DeviceCommunication deviceCommunication;
        private StreamingConnection streamingConnection;
        private DeviceConnection deviceConnection;
        private DiscoveredSsdpDevice discoveredSsdpDevice;
        private SsdpDevice ssdpDevice;
        private DeviceState deviceState;
        private DeviceControl deviceControl;
        private MenuItem menuItem;
        private Volume volumeSetting;
        private DateTime lastVolumeChange;
        private string streamingUrl;

        public Device(DeviceConnection deviceConnectionIn, DeviceCommunication deviceCommunicationIn)
        {
            deviceConnection = deviceConnectionIn;
            deviceConnection.SetCallback(GetHost, SetDeviceState, OnReceiveMessage);
            deviceCommunication = deviceCommunicationIn;
            deviceCommunication.SetCallback(SetDeviceState, OnVolumeUpdate, deviceConnection.SendMessage, GetDeviceState, IsConnected, deviceConnection.IsConnected, GetHost);
            deviceState = DeviceState.NotConnected;
            volumeSetting = new Volume
            {
                controlType = "attenuation",
                level = 0.0f,
                muted = false,
                stepInterval = 0.05f
            };
        }

        public void SetDiscoveredDevices(DiscoveredSsdpDevice discoveredSsdpDeviceIn, SsdpDevice ssdpDeviceIn)
        {
            discoveredSsdpDevice = discoveredSsdpDeviceIn;
            ssdpDevice = ssdpDeviceIn;
        }

        public void OnClickDeviceButton(object sender, EventArgs e)
        {
            deviceCommunication.OnClickDeviceButton(deviceState, streamingUrl);
        }

        public void Start()
        {
            deviceCommunication.LoadMedia(streamingUrl);
        }

        public void OnRecordingDataAvailable(ArraySegment<byte> dataToSend, WaveFormat format)
        {
            if (streamingConnection != null)
            {
                if (streamingConnection.IsConnected())
                {
                    if (deviceState == DeviceState.Buffering || deviceState == DeviceState.Playing)
                        streamingConnection.SendData(dataToSend, format);
                }
                else
                {
                    Console.WriteLine(string.Format("Connection closed from {0}", streamingConnection.GetRemoteEndPoint()));
                    streamingConnection = null;
                }
            }
        }

        public void OnGetStatus()
        {
            deviceCommunication.GetMediaStatus();
        }

        public void SetDeviceState(DeviceState state, string text = null)
        {
            deviceState = state;
            deviceControl?.SetStatus(state, text);
        }

        public void SetStreamingUrl(string streamingUrl)
        {
            this.streamingUrl = streamingUrl;
        }

        public bool IsConnected()
        {
            return !(deviceState.Equals(DeviceState.NotConnected) ||
                deviceState.Equals(DeviceState.ConnectError) ||
                deviceState.Equals(DeviceState.Closed));
        }

        public void OnVolumeUpdate(Volume volume)
        {
            volumeSetting = volume;
            deviceControl?.OnVolumeUpdate(volume);
        }

        public void VolumeSet(float level)
        {
            if (!IsConnected())
                return;

            if (lastVolumeChange != null && DateTime.Now.Ticks - lastVolumeChange.Ticks < 1000)
                return;

            lastVolumeChange = DateTime.Now;

            if (volumeSetting.level > level)
                while (volumeSetting.level > level) volumeSetting.level -= volumeSetting.stepInterval;
            if (volumeSetting.level < level)
                while (volumeSetting.level < level) volumeSetting.level += volumeSetting.stepInterval;
            if (level > 1) level = 1;
            if (level < 0) level = 0;

            volumeSetting.level = level;
            deviceCommunication.VolumeSet(volumeSetting);
        }

        public void VolumeUp()
        {
            if (!IsConnected())
                return;

            VolumeSet(volumeSetting.level + 0.05f);
        }

        public void VolumeDown()
        {
            if (!IsConnected())
                return;

            VolumeSet(volumeSetting.level - 0.05f);
        }

        public void VolumeMute()
        {
            if (!IsConnected())
                return;

            deviceCommunication.VolumeMute(!volumeSetting.muted);
        }

        public void Stop()
        {
            deviceCommunication.Stop();
            SetDeviceState(DeviceState.Closed);
        }

        public bool AddStreamingConnection(string remoteAddress, Socket socket)
        {
            if (discoveredSsdpDevice.DescriptionLocation.Host.Equals(remoteAddress))
            {
                streamingConnection = new StreamingConnection();
                streamingConnection.SetSocket(socket);
                streamingConnection.SendStartStreamingResponse();
                return true;
            }

            return false;
        }

        public string GetUsn()
        {
            if (discoveredSsdpDevice != null)
                return discoveredSsdpDevice.Usn;

            return string.Empty;
        }

        public string GetHost()
        {
            if (discoveredSsdpDevice != null)
                return discoveredSsdpDevice.DescriptionLocation.Host;

            return string.Empty;
        }

        public string GetFriendlyName()
        {
            if (ssdpDevice != null)
                return ssdpDevice.FriendlyName;

            return string.Empty;
        }

        public DeviceState GetDeviceState()
        {
            return deviceState;
        }

        public DeviceControl GetDeviceControl()
        {
            return deviceControl;
        }

        public void SetDeviceControl(DeviceControl deviceControlIn)
        {
            deviceControl = deviceControlIn;
        }

        public void SetMenuItem(MenuItem menuItemIn)
        {
            menuItem = menuItemIn;
        }

        public MenuItem GetMenuItem()
        {
            return menuItem;
        }

        public DeviceConnection GetDeviceConnection()
        {
            return deviceConnection;
        }

        public void OnReceiveMessage(CastMessage castMessage)
        {
            deviceCommunication?.OnReceiveMessage(castMessage, streamingUrl);
        }
    }
}
