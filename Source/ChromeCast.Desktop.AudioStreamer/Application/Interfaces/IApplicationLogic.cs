﻿using System;
using System.Net;
using System.Net.Sockets;
using NAudio.Wave;

namespace ChromeCast.Desktop.AudioStreamer.Application.Interfaces
{
    public interface IApplicationLogic
    {
        void Start();
        string GetStreamingUrl();
        void SetLagThreshold(int lagThreshold);
        void OnAddDevice(IDevice device);
        void OnSetHooks(bool @checked);
        void OnRecordingDataAvailable(ArraySegment<byte> dataToSend, WaveFormat waveFormat);
        void OnStreamingRequestsListen(string host, int port);
        void OnStreamingRequestConnect(Socket handlerSocket, string httpRequest);
        void SetDependencies(MainForm mainForm);
        void CloseApplication();
        void RecordingDeviceChanged();
        void OnSetAutoRestart(bool autoRestart);
        bool GetAutoRestart();
        void ChangeIPAddressUsed(IPAddress ipAddress);
        void ScanForDevices();
    }
}