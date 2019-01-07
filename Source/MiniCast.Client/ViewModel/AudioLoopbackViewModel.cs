using ChromeCast.Library.Streaming;
using CSCore;
using CSCore.Streams;
using GalaSoft.MvvmLight;
using SpectrumAnalyzer.Models;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MiniCast.Client.ViewModel
{
    public class AudioLoopbackViewModel : RootViewModelBase
    {
        private LoopbackRecorder loopbackRecorder;

        public event Action<ArraySegment<byte>, WaveFormat> RecordingDataAvailable;
        public AnalyzerViewModel Analyzer { get; private set; }

        public event Action<ObservableCollection<FrequencyBin>, double> BinsUpdated;

        public AudioLoopbackViewModel()
        {
            loopbackRecorder = new LoopbackRecorder();
            loopbackRecorder.StartRecording(LoopbackRecorder.GetDevices().defaultDevice, (ArraySegment<byte> dataToSend, WaveFormat format) =>
            {
                RecordingDataAvailable?.Invoke(dataToSend, format);

                if (Analyzer != null)
                {
                    var dataSize = dataToSend.Count;
                    var tempData = ArrayPool<byte>.Shared.Rent(dataSize);
                    Buffer.BlockCopy(dataToSend.Array, dataToSend.Offset, tempData, 0, dataSize);
                    var tempDataView = new ArraySegment<byte>(tempData, 0, dataSize);

                    Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, (Action)(() =>
                    {
                        Analyzer.AddData(tempDataView, format);

                        ArrayPool<byte>.Shared.Return(tempData);
                    }));
                }
            });

            Analyzer = new AnalyzerViewModel(loopbackRecorder.WaveFormat);
            Analyzer.BinsUpdated += Analyzer_BinsUpdated;
        }

        private void Analyzer_BinsUpdated(ObservableCollection<FrequencyBin> bins, double maxValue)
        {
            BinsUpdated?.Invoke(bins, maxValue);
        }

        public override void Cleanup()
        {
            loopbackRecorder.StopRecording();

            Analyzer.Cleanup();

            base.Cleanup();
        }
    }
}
