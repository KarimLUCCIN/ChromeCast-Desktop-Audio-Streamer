using GalaSoft.MvvmLight;
using MiniCast.Client.Helpers;
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

            var gradients = MusicColor.ColorGradient.OrderedStops.ToArray();
            int gradientIndex = 0;

            if (gradients.Length > 1)
            {
                int count = bins.Count;
                for (int i = 0; i < count; i++)
                {
                    float currentOffset = (i / (float)(count - 1));

                    Color stopColor;

                    while (true)
                    {
                        float currentGradientStart = gradientIndex / (float)(gradients.Length - 1);
                        float currentGradientStop = (gradientIndex + 1) / (float)(gradients.Length - 1);

                        if (currentOffset >= 1 || currentGradientStart >= 1 || gradientIndex >= gradients.Length - 2)
                        {
                            stopColor = gradients.Last().Color;
                            break;
                        }
                        else
                        {
                            if (currentOffset >= currentGradientStart && currentOffset <= currentGradientStop)
                            {
                                float subOffset = (currentGradientStop - currentOffset) / (currentGradientStop - currentGradientStart);
                                stopColor = ColorHelpers.Lerp(gradients[gradientIndex].Color, gradients[gradientIndex + 1].Color, subOffset);
                                break;
                            }
                            else
                            {
                                gradientIndex++;
                            }
                        }
                    }

                    Vector4 colorVec = new Vector4(stopColor.ScR, stopColor.ScG, stopColor.ScB, 1.0f);

                    float binAmount = (float)(bins[bins.Count - i - 1].Value / maxValue);

                    totalColor += colorVec * 16 * stopColor.ScA * binAmount;
                }
            }

            var finalColor = baseColor + totalColor / bins.Count;

            CurrentColor = new Color() { ScR = finalColor.X, ScG = finalColor.Y, ScB = finalColor.Z, ScA = 1.0f };
            CurrentColorVersion++;
        }
    }
}
