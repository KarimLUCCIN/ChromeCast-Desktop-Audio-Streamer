﻿using System;
using System.Net.Sockets;
using System.Windows.Forms;
using Rssdp;
using NAudio.Wave;
using ChromeCast.Desktop.AudioStreamer.UserControls;
using ChromeCast.Desktop.AudioStreamer.Communication;
using ChromeCast.Desktop.AudioStreamer.Communication.Classes;
using ChromeCast.Desktop.AudioStreamer.ProtocolBuffer;

namespace ChromeCast.Desktop.AudioStreamer.Application
{
    public interface IDevice
    {
        bool IsConnected();
        void SetDeviceState(DeviceState disposed, string text = null);
        void SetDiscoveredDevices(DiscoveredSsdpDevice device, SsdpDevice fullDevice);
        bool AddStreamingConnection(string remoteAddress, Socket socket);
        void OnGetStatus();
        void OnRecordingDataAvailable(ArraySegment<byte> dataToSend, WaveFormat format, int reduceLagThreshold);
        void OnClickDeviceButton(object sender, EventArgs e);
        string GetUsn();
        string GetHost();
        string GetFriendlyName();
        DeviceState GetDeviceState();
        void SetDeviceControl(DeviceControl deviceControl);
        MenuItem GetMenuItem();
        void SetMenuItem(MenuItem menuItem);
        void OnVolumeUpdate(Volume volume);
        void VolumeUp();
        void VolumeDown();
        void VolumeMute();
        void VolumeSet(float level);
        void Stop();
        void Start();
        void OnReceiveMessage(CastMessage castMessage);
        DeviceControl GetDeviceControl();
    }
}