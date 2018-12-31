﻿using System;
using NAudio.Wave;

namespace ChromeCast.Desktop.AudioStreamer.Streaming.Interfaces
{
    public interface ILoopbackRecorder
    {
        void StartRecording(Action<ArraySegment<byte>, WaveFormat> dataAvailableCallback);
        void StartRecordingDevice();
        void StopRecording();
        void GetDevices(IMainForm mainForm);
    }
}