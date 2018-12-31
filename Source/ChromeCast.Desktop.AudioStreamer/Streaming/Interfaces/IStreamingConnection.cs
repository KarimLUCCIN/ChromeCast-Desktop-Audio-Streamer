﻿using System;
using System.Net.Sockets;
using NAudio.Wave;

namespace ChromeCast.Desktop.AudioStreamer.Streaming.Interfaces
{
    public interface IStreamingConnection
    {
        void SendData(ArraySegment<byte> dataToSend, WaveFormat format, int reduceLagThreshold);
        void SendStartStreamingResponse();
        bool IsConnected();
        void SetSocket(Socket socket);
        string GetRemoteEndPoint();
    }
}