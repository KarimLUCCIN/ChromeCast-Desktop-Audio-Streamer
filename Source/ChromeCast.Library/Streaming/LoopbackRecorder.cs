using System;
using System.Linq;
using System.Collections.Generic;
using CSCore.CoreAudioAPI;
using CSCore.SoundIn;
using CSCore.Streams;
using CSCore;
using System.Threading;
using System.Diagnostics;

namespace ChromeCast.Library.Streaming
{
    public class LoopbackRecorder
    {
        WasapiCapture soundIn;
        private Action<ArraySegment<byte>, CSCore.WaveFormat> dataAvailableCallback;
        private bool isRecording = false;
        IWaveSource convertedSource;
        SoundInSource soundInSource;
        CSCore.WaveFormat waveFormat;

        public WaveFormat WaveFormat => waveFormat;
        public IWaveSource WaveSource => convertedSource;
        public SoundInSource SoundInSource => soundInSource;

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
            lock (bufferSwapSync)
            {
                var tmp = buffer0;
                buffer0 = buffer1;
                buffer1 = tmp;
            }
        }

        ~LoopbackRecorder()
        {
            StopRecording();
        }

        public void StartRecording(MMDevice device, Action<ArraySegment<byte>, CSCore.WaveFormat> dataAvailableCallbackIn)
        {
            if (isRecording)
                return;

            dataAvailableCallback = dataAvailableCallbackIn;

            StartRecordingDevice(device);
            isRecording = true;
        }

        public void StartRecordingDevice(MMDevice recordingDevice)
        {
            if (recordingDevice == null)
            {
                Console.WriteLine("No devices found.");
                return;
            }

            StopRecording();

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
            
            waveFormat = convertedSource.WaveFormat;

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

                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
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
            enabled = false;
            if (eventThread != null)
            {
                eventThread.Join();
            }
            eventThread = null;

            isRecording = false;
            if (soundIn != null)
            {
                soundIn.Stop();
                soundIn.Dispose();
                soundIn = null;
            }
        }

        public static (IEnumerable<MMDevice> devices, MMDevice defaultDevice) GetDevices()
        {
            var defaultDevice = MMDeviceEnumerator.DefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            var devices = MMDeviceEnumerator.EnumerateDevices(DataFlow.Render, DeviceState.Active);

            return (devices, defaultDevice);
        }
    }
}