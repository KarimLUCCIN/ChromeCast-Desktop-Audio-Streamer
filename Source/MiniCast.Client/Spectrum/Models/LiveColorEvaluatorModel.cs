using GalaSoft.MvvmLight;
using MiniCast.Client.ViewModel;
using SpectrumAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace MiniCast.Client.Spectrum.Models
{
    public class LiveColorEvaluatorModel : ViewModelBase, INotifyPropertyChanged
    {
        public AudioLoopbackViewModel AudioLoopback { get; } = ViewModelLocator.Instance.LoopbackRecorder;
        public MusicColorViewModel MusicColor { get; } = ViewModelLocator.Instance.MusicColor;

        public Color CurrentColor { get; private set; }
        public ulong CurrentColorVersion { get; private set; }

        public LiveColorEvaluatorModel()
        {
            AudioLoopback.BinsUpdated += AudioLoopback_BinsUpdated;

            CurrentColor = MusicColor.BaseColor;
        }

        private void AudioLoopback_BinsUpdated(ObservableCollection<FrequencyBin> bins, double maxValue)
        {
            if (bins == null || bins.Count < 2)
            {
                CurrentColor = MusicColor.BaseColor;
                return;
            }

            var baseColor = new Vector4(MusicColor.BaseColor.ScR, MusicColor.BaseColor.ScG, MusicColor.BaseColor.ScB, 1.0f);
            var totalColor = Vector4.Zero; // who cares about double precision?

            var lowColor = new Vector4(MusicColor.LowOctavesColor.ScR, MusicColor.LowOctavesColor.ScG, MusicColor.LowOctavesColor.ScB, 1.0f);
            var highColor = new Vector4(MusicColor.HighOctavesColor.ScR, MusicColor.HighOctavesColor.ScG, MusicColor.HighOctavesColor.ScB, 1.0f);

            int count = bins.Count;
            for (int i = 0; i < count; i++)
            {
                var lerpedColor = Vector4.Lerp(lowColor, highColor, (i / (float)(count - 1)));
                totalColor += Vector4.Lerp(baseColor, lerpedColor, (float)(bins[i].Value / maxValue));
            }

            var finalColor = totalColor / bins.Count;

            CurrentColor = new Color() { ScR = finalColor.X, ScG = finalColor.Y, ScB = finalColor.Z, ScA = 1.0f };
            CurrentColorVersion++;
        }
    }
}
