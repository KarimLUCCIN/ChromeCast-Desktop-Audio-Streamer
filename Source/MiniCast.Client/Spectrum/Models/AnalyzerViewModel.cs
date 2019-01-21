using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using CSCore;
using CSCore.CoreAudioAPI;
using CSCore.DSP;
using CSCore.SoundIn;
using CSCore.Streams;
using GalaSoft.MvvmLight;
using SpectrumAnalyzer.Enums;

namespace SpectrumAnalyzer.Models
{
    public class AnalyzerViewModel : ViewModelBase, INotifyPropertyChanged
    {
        /// <summary>
        ///     captures the default devices audio stream, performs an fft and stores the result in <see cref="FrequencyBins" />
        /// </summary>
        /// <param name="bins">number of frequency bins in <see cref="FrequencyBins" /></param>
        /// <param name="rate">number of refreshes per second</param>
        /// <param name="normal">normalized values in <see cref="FrequencyBins" /></param>
        public AnalyzerViewModel(WaveFormat format, int bins = 50, int rate = 50, int normal = 255)
        {
            Bins = bins;
            Rate = rate;
            Normal = normal;
            DetectBeats = true;

            Initialize(format);
        }

        public override void Cleanup()
        {
            cleanedUp = true;

            base.Cleanup();
        }

        #region Fields

        private bool cleanedUp = false;
        private const FftSize FftSize = CSCore.DSP.FftSize.Fft4096;
        private const int ScaleFactorLinear = 9;
        private const int ScaleFactorSqrt = 2;
        private const double MinDbValue = -90;
        private const double MaxDbValue = 0;
        private const double DbScale = MaxDbValue - MinDbValue;

        private readonly Timer _updateSpectrumTimer = new Timer();
        private SpectrumProvider _spectrumProvider;
        private float[] _spectrumData;
        private Queue<float[]> _history;
        private int _minimumFrequencyIndex;
        private int _maximumFrequencyIndex;
        private int[] _spectrumLinearScaleIndexMax;
        private int[] _spectrumLogarithmicScaleIndexMax;

        private int _rate;
        private int _bins;
        private bool _detectBeats;

        #endregion Fields

        #region Properties

        #region Input

        #region Frequency Analysis

        public int Bins
        {
            get => _bins;
            set
            {
                _bins = value;
                FrequencyBins = new ObservableCollection<FrequencyBin>(Enumerable.Range(0, Bins).Select(i => new FrequencyBin(i)));
                RaisePropertyChanged();
            }
        }

        public int Rate
        {
            get => _rate;
            set
            {
                _rate = value;
                _updateSpectrumTimer.Interval = 1000.0 / value;
                RaisePropertyChanged();
            }
        }

        public int Normal { get; set; }

        public int MinFrequency { get; set; } = 20;

        public int MaxFrequency { get; set; } = 5000;

        public ScalingStrategy ScalingStrategy { get; set; } = ScalingStrategy.Sqrt;

        public bool LogarithmicX { get; set; } = true;

        public bool Average { get; set; }

        #endregion Frequency Analysis

        #region Beat Detection

        public bool DetectBeats
        {
            get => _detectBeats;
            set
            {
                _detectBeats = value;
                if (value)
                    _updateSpectrumTimer.Elapsed += DetectObserverBand;
                else
                    _updateSpectrumTimer.Elapsed -= DetectObserverBand;
            }
        }

        public double BeatSensibility { get; set; }

        #endregion Beat Detection

        #endregion Input

        #region Output

        #region Frequency Analysis

        public ObservableCollection<FrequencyBin> FrequencyBins { get; private set; } = new ObservableCollection<FrequencyBin>();

        public AudioEndpointVolume AudioEndpointVolume { get; set; }

        #endregion Frequency Analysis

        #region Beat Detection

        public ObservableCollection<FrequencyObserver> FrequencyObservers { get; } =
            new ObservableCollection<FrequencyObserver>
            {
                // TODO load from file + editable
                new FrequencyObserver {Title = "Treble", MinFrequency = 5200, MaxFrequency = 20000},
                new FrequencyObserver {Title = "Mid", MinFrequency = 400, MaxFrequency = 5200},
                new FrequencyObserver {Title = "Bass", MinFrequency = 20, MaxFrequency = 400},
                new FrequencyObserver {Title = "Kick", MinFrequency = 108-30, MaxFrequency = 108+30, PitchColor = Brushes.White}
            };

        #endregion Beat Detection

        #endregion Output

        #region Private

        private static int SpectrumDataSize => (int)FftSize / 2 - 1;

        #endregion

        #endregion

        #region Private Methods

        private void Initialize(WaveFormat format)
        {
            InitializeCapture(format);
            _updateSpectrumTimer.Elapsed += UpdateSpectrum;
            _updateSpectrumTimer.AutoReset = false;

            _updateSpectrumTimer.Start();
        }

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

        private void InitializeCapture(WaveFormat format)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                FrequencyBins.Clear();
                foreach (var frequencyBin in Enumerable.Range(0, Bins).Select(i => new FrequencyBin(i)))
                {
                    FrequencyBins.Add(frequencyBin);
                }
            });

            _spectrumData = new float[(int)FftSize];
            _history = new Queue<float[]>(_rate);

            _spectrumProvider = new SpectrumProvider(format.Channels, format.SampleRate, FftSize);
            UpdateFrequencyMapping();
        }

        public void AddData(ArraySegment<byte> data, WaveFormat format)
        {
            var currentDataPtr = data;
            for (int i = 0; i < data.Count;)
            {
                float left = 0;
                float right = 0;

                for (int j = 0; j < format.Channels; j++)
                {
                    var step = ConvertToSample(currentDataPtr, format.BitsPerSample, mindown: true);
                    i += step.increment;

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

                _spectrumProvider.Add(left, right);
            }
        }

        #endregion

        #region Frequencies

        // based on the https://github.com/filoe/cscore visualization example
        private void UpdateFrequencyMapping()
        {
            _minimumFrequencyIndex = Math.Min(_spectrumProvider.GetFftBandIndex(MinFrequency), SpectrumDataSize);
            _maximumFrequencyIndex = Math.Min(_spectrumProvider.GetFftBandIndex(MaxFrequency) + 1, SpectrumDataSize);

            var indexCount = _maximumFrequencyIndex - _minimumFrequencyIndex;
            var linearIndexBucketSize = Math.Round(indexCount / (double)_bins, 3);

            _spectrumLinearScaleIndexMax = _spectrumLinearScaleIndexMax.CheckBuffer(_bins, true);
            _spectrumLogarithmicScaleIndexMax = _spectrumLogarithmicScaleIndexMax.CheckBuffer(_bins, true);

            var maxLog = Math.Log(_bins, _bins);
            for (var i = 1; i <= _bins; i++)
            {
                var map = i - 1;
                var logIndex =
                    (int)((maxLog - Math.Log(_bins + 1 - i, _bins + 1)) * indexCount) +
                    _minimumFrequencyIndex;

                _spectrumLinearScaleIndexMax[map] = _minimumFrequencyIndex + (int)(i * linearIndexBucketSize);
                _spectrumLogarithmicScaleIndexMax[map] = logIndex;

                if (FrequencyBins is null) continue; // apply band to bin:
                var relatedBin = FrequencyBins[map];

                relatedBin.MinFrequency = map > 0
                    ? _spectrumProvider.GetFrequency(LogarithmicX
                        ? _spectrumLogarithmicScaleIndexMax[map - 1]
                        : _spectrumLinearScaleIndexMax[map - 1]) + 1
                    : MinFrequency;
                relatedBin.MaxFrequency = map < _bins - 1
                    ? _spectrumProvider.GetFrequency(LogarithmicX
                        ? _spectrumLogarithmicScaleIndexMax[map]
                        : _spectrumLinearScaleIndexMax[map])
                    : MaxFrequency;
            }

            if (_bins > 0)
                _spectrumLinearScaleIndexMax[_spectrumLinearScaleIndexMax.Length - 1] =
                    _spectrumLogarithmicScaleIndexMax[_spectrumLogarithmicScaleIndexMax.Length - 1] = _maximumFrequencyIndex;
        }

        double[] frequencyBins = null;

        public event Action<ObservableCollection<FrequencyBin>, double> BinsUpdated;

        // based on the https://github.com/filoe/cscore visualization example
        private void UpdateSpectrum(object sender, EventArgs e)
        {
            if (cleanedUp)
            {
                return;
            }

            try
            {
                if (!_spectrumProvider.IsNewDataAvailable)
                {
                    Application.Current?.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        foreach (var frequencyBin in FrequencyBins) frequencyBin.Value = 0;

                        BinsUpdated?.Invoke(FrequencyBins, Normal);
                    }));
                    return;
                }

                _spectrumProvider.GetFftData(_spectrumData);

                double value0 = 0, value = 0;
                double lastValue = 0;
                double actualMaxValue = Normal;
                var spectrumPointIndex = 0;

                if (frequencyBins == null || frequencyBins.Length != Bins)
                {
                    frequencyBins = new double[Bins];
                }

                for (var i = _minimumFrequencyIndex; i <= _maximumFrequencyIndex; i++)
                {
                    switch (ScalingStrategy)
                    {
                        case ScalingStrategy.Decibel:
                            value0 = (20 * Math.Log10(_spectrumData[i]) - MinDbValue) / DbScale * actualMaxValue;
                            break;
                        case ScalingStrategy.Linear:
                            value0 = _spectrumData[i] * ScaleFactorLinear * actualMaxValue;
                            break;
                        case ScalingStrategy.Sqrt:
                            value0 = Math.Sqrt(_spectrumData[i]) * ScaleFactorSqrt * actualMaxValue;
                            break;
                    }

                    var recalc = true;
                    value = Math.Max(0, Math.Max(value0, value));

                    while (spectrumPointIndex <= _spectrumLinearScaleIndexMax.Length - 1 &&
                           i == (LogarithmicX
                               ? _spectrumLogarithmicScaleIndexMax[spectrumPointIndex]
                               : _spectrumLinearScaleIndexMax[spectrumPointIndex]))
                    {
                        if (!recalc)
                            value = lastValue;

                        if (value > Normal)
                            value = Normal;

                        if (Average && spectrumPointIndex > 0)
                            value = (lastValue + value) / 2.0;

                        frequencyBins[spectrumPointIndex] = value;
                        lastValue = value;
                        value = 0.0;
                        spectrumPointIndex++;
                        recalc = false;
                    }
                }

                var disp = Application.Current?.Dispatcher;
                if (disp != null)
                {
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        var index = 0;
                        foreach (var frequencyBin in FrequencyBins)
                        {
                            var oldFreq = frequencyBin.Value;
                            var newFreq = frequencyBins[index++];
                            var finalFreq = newFreq;

                            if (oldFreq > newFreq)
                            {
                                const float smooth = .95f;
                                finalFreq = smooth * oldFreq + (1 - smooth) * newFreq;
                            }

                            double dist = Math.Abs(newFreq - oldFreq);
                            if (dist < .1)
                            {
                                const float smooth = .99f;
                                finalFreq = smooth * oldFreq + (1 - smooth) * newFreq;
                            }

                            frequencyBin.Value = finalFreq;
                        }

                        BinsUpdated?.Invoke(FrequencyBins, actualMaxValue);
                    }));
                }
            }
            finally
            {
                _updateSpectrumTimer.Start();
            }
        }

        #endregion

        #region Observers

        private void UpdateHistory()
        {
            if (_history.Count >= _rate) _history.Dequeue();
            _history.Enqueue(_spectrumData.ToArray());
        }

        private float[] CalculateAverages()
        {
            var avg = new float[SpectrumDataSize];
            if (_history.Count < _rate) return avg;

            for (var frequencyIndex = 0; frequencyIndex < SpectrumDataSize; frequencyIndex++)
                avg[frequencyIndex] = _history.Sum(spectrum => spectrum[frequencyIndex]) / _rate;

            return avg;
        }

        private float GetFrequencyPool(IReadOnlyList<float> spectrum, int from, int to)
        {
            var avgFromTo = 0f;
            var minFreqIndex = Math.Min(_spectrumProvider.GetFftBandIndex(from), SpectrumDataSize);
            var maxFreqIndex = Math.Min(_spectrumProvider.GetFftBandIndex(to) + 1, SpectrumDataSize);
            if (minFreqIndex > maxFreqIndex) return avgFromTo;

            for (var frequencyIndex = minFreqIndex; frequencyIndex < maxFreqIndex; frequencyIndex++)
                avgFromTo += spectrum[frequencyIndex];
            return avgFromTo / (maxFreqIndex - minFreqIndex);
        }

        private void DetectObserverBand(object sender, EventArgs e)
        {
            var historyAverage = CalculateAverages();
            foreach (var fo in FrequencyObservers)
            {
                var cur = GetFrequencyPool(_spectrumData, fo.MinFrequency, fo.MaxFrequency);
                if (_history.Count < _rate) continue;

                Application.Current?.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    fo.AdjustAverage(cur);
                    var avg = GetFrequencyPool(historyAverage, fo.MinFrequency, fo.MaxFrequency);
                    fo.BeatDetected = cur > fo.AverageEnergyThreshold && cur > avg * fo.AverageFactor;
                }));
            }
            UpdateHistory();
        }

        #endregion
    }
}