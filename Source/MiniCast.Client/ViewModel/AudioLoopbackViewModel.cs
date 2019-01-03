using ChromeCast.Library.Streaming;
using CSCore;
using CSCore.Streams;
using GalaSoft.MvvmLight;
using MiniCast.Client.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniCast.Client.ViewModel
{
    public class AudioLoopbackViewModel : ViewModelBase
    {
        private LoopbackRecorder loopbackRecorder;

        public event Action<ArraySegment<byte>, WaveFormat> RecordingDataAvailable;
        public BasicSpectrumProvider Spectrum { get; private set; }

        private float[] fftData;

        private static float Bit8ToFloat(ArraySegment<byte> buffer, bool minDown)
        {
            return buffer.Array[buffer.Offset] / (minDown ? (128.0f - 1.0f) : 1.0f);
        }

        private static float Bit16ToFloat(ArraySegment<byte> buffer, bool minDown)
        {
            return BitConverter.ToInt16(buffer.Array, buffer.Offset) / (minDown ? 32768f : 1.0f);
        }

        private static float Bit32ToFloat(ArraySegment<byte> buffer, bool minDown)
        {
            return BitConverter.ToInt32(buffer.Array, buffer.Offset) / (minDown ? 2147483648f : 1.0f);
        }

        private static float Bit24ToFloat(ArraySegment<byte> buffer, bool minDown)
        {
            //byte 3 << 16 , byte 2 << 8 byte 1 , 8388608f = 2^24/2
            return (((sbyte)buffer.Array[buffer.Offset + 2] << 16) |
                        (buffer.Array[buffer.Offset + 1] << 8) |
                        buffer.Array[buffer.Offset]) / (minDown ? 8388608f : 1.0f);
        }

        private static (float sample, int increment) ConvertToSample(ArraySegment<byte> buffer, int bitsPerSample, bool mindown)
        {
            float value;
            if (bitsPerSample == 8)
                value = Bit8ToFloat(buffer, mindown);
            else if (bitsPerSample == 16)
                value = Bit16ToFloat(buffer, mindown);
            else if (bitsPerSample == 24)
                value = Bit24ToFloat(buffer, mindown);
            else if (bitsPerSample == 32)
                value = Bit32ToFloat(buffer, mindown);
            else
                throw new ArgumentOutOfRangeException("bitsPerSample");

            return (value, (bitsPerSample / 8));
        }

        public AudioLoopbackViewModel()
        {
            loopbackRecorder = new LoopbackRecorder();
            loopbackRecorder.StartRecording(LoopbackRecorder.GetDevices().defaultDevice, (ArraySegment<byte> dataToSend, WaveFormat format) =>
            {
                if (Spectrum != null)
                {
                    var currentDataPtr = dataToSend;
                    for (int i = 0; i < dataToSend.Count;)
                    {
                        float left = 0;
                        float right = 0;

                        for (int j = 0; j < format.Channels; j++)
                        {
                            var step = ConvertToSample(currentDataPtr, format.BitsPerSample, mindown: true);
                            i += format.BitsPerSample;

                            if (j % 2 == 0)
                            {
                                left += step.sample;
                            }
                            else
                            {
                                right += step.sample;
                            }

                            currentDataPtr = new ArraySegment<byte>(currentDataPtr.Array, currentDataPtr.Offset + step.increment, currentDataPtr.Count - step.increment);
                        }

                        if (format.Channels > 1)
                        {
                            left /= format.Channels / 2;
                            right /= format.Channels / 2;
                        }

                        Spectrum.Add(left, right);
                    }
                }

                RecordingDataAvailable?.Invoke(dataToSend, format);
            });

            Spectrum = new BasicSpectrumProvider(loopbackRecorder.WaveFormat.Channels, loopbackRecorder.WaveFormat.SampleRate, CSCore.DSP.FftSize.Fft4096);
            fftData = new float[(int)Spectrum.FftSize];
        }

        public override void Cleanup()
        {
            loopbackRecorder.StopRecording();

            base.Cleanup();
        }
    }
}
