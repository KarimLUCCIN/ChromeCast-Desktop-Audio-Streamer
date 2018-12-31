using System;
using System.Linq;
using System.Collections.Generic;
using ChromeCast.Desktop.AudioStreamer.Streaming.Interfaces;
using NAudio.Wave;
using CSCore.CoreAudioAPI;
using CSCore.SoundIn;
using CSCore.Streams;
using CSCore;
using System.Threading;
using System.Diagnostics;

namespace ChromeCast.Desktop.AudioStreamer.Streaming
{
    public class LoopbackRecorder : ILoopbackRecorder
    {
        WasapiCapture soundIn;
        private Action<ArraySegment<byte>, NAudio.Wave.WaveFormat> dataAvailableCallback;
        private bool isRecording = false;
        IWaveSource convertedSource;
        SoundInSource soundInSource;
        NAudio.Wave.WaveFormat waveFormat;
        IMainForm mainForm;

        class BufferBlock
        {
            public byte[] Data;
            public int Used;
        }

        BufferBlock buffer0, buffer1;
        object bufferSwapSync = new object();
        Thread eventThread;
        bool enabled = false;

        private void SwapBuffer()
        {
            lock(bufferSwapSync)
            {
                var tmp = buffer0;
                buffer0 = buffer1;
                buffer1 = tmp;
            }
        }

        ~LoopbackRecorder()
        {
            enabled = false;
            if (eventThread != null)
            {
                eventThread.Join();
            }
        }

        public void StartRecording(Action<ArraySegment<byte>, NAudio.Wave.WaveFormat> dataAvailableCallbackIn)
        {
            if (isRecording)
                return;

            dataAvailableCallback = dataAvailableCallbackIn;

            StartRecordingDevice();
            isRecording = true;
        }

        public void StartRecordingDevice()
        {
            MMDevice recordingDevice = mainForm.GetRecordingDevice();
            if (recordingDevice == null)
            {
                Console.WriteLine("No devices found.");
                return;
            }

            soundIn = new CSCore.SoundIn.WasapiLoopbackCapture()
            {
                Device = recordingDevice
            };

            soundIn.Initialize();
            soundInSource = new SoundInSource(soundIn) { FillWithZeros = false };
            convertedSource = soundInSource.ChangeSampleRate(44100).ToSampleSource().ToWaveSource(16);
            convertedSource = convertedSource.ToStereo();
            soundInSource.DataAvailable += OnDataAvailable;
            soundIn.Start();

            var format = convertedSource.WaveFormat;
            waveFormat = NAudio.Wave.WaveFormat.CreateCustomFormat(WaveFormatEncoding.Pcm, format.SampleRate, format.Channels, format.BytesPerSecond, format.BlockAlign, format.BitsPerSample);

            buffer0 = new BufferBlock() { Data = new byte[convertedSource.WaveFormat.BytesPerSecond / 2] };
            buffer1 = new BufferBlock() { Data = new byte[convertedSource.WaveFormat.BytesPerSecond / 2] };

            enabled = true;

            eventThread = new Thread(EventThread);
            eventThread.Name = "Loopback Event Thread";
            eventThread.IsBackground = true;
            eventThread.Start(new WeakReference<LoopbackRecorder>(this));
        }

        private static void EventThread(object param)
        {
            var thisRef = (WeakReference<LoopbackRecorder>)param;
            try
            {
                while (true)
                {
                    LoopbackRecorder recorder = null;
                    if (!thisRef.TryGetTarget(out recorder) || recorder == null)
                    {
                        // Instance is dead
                        return;
                    }

                    if (!recorder.enabled)
                    {
                        return;
                    }

                    recorder.SwapBuffer();
                    if (recorder.buffer1.Used > 0)
                    {
                        recorder.dataAvailableCallback(new ArraySegment<byte>(recorder.buffer1.Data, 0, recorder.buffer1.Used), recorder.waveFormat);
                        recorder.buffer1.Used = 0;
                    }

                    Thread.Sleep(1);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        private void OnDataAvailable(object sender, DataAvailableEventArgs e)
        {
            if (dataAvailableCallback != null)
            {
                int read;

                lock (bufferSwapSync)
                {
                    var currentBuffer = buffer0;
                    var spaceLeft = buffer0.Data.Length - buffer0.Used;

                    while (spaceLeft > 0 && (read = convertedSource.Read(currentBuffer.Data, currentBuffer.Used, spaceLeft)) > 0)
                    {
                        spaceLeft -= read;
                        currentBuffer.Used += read;
                    }
                }
            }
        }

        public void StopRecording()
        {
            isRecording = false;
            if (soundIn != null)
            {
                soundIn.Stop();
            }
        }

        private void OnRecordingStopped(object sender, CSCore.StoppedEventArgs eventArgs)
        {
            enabled = false;
            eventThread.Join();
            eventThread = null;

            if (soundIn != null)
            {
                soundIn.Dispose();
                soundIn = null;
            }
            isRecording = false;

            if (eventArgs.Exception != null)
            {
                throw eventArgs.Exception;
            }
        }

        public void GetDevices(IMainForm mainFormIn)
        {
            mainForm = mainFormIn;
            var defaultDevice = MMDeviceEnumerator.DefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            var devices = MMDeviceEnumerator.EnumerateDevices(DataFlow.Render, DeviceState.Active);
            mainForm.AddRecordingDevices(devices, defaultDevice);
        }
    }
}