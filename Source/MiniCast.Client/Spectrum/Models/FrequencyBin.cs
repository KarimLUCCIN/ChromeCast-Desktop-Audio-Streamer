using GalaSoft.MvvmLight;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Media;

namespace SpectrumAnalyzer.Models
{
    [DebuggerDisplay("{MinFrequency} - {MaxFrequency}Hz")]
    public class FrequencyBin : ViewModelBase, INotifyPropertyChanged
    {
        #region Properties

        public FrequencyBin(int value = 0)
        {
            Value = value;
        }

        public double Value { get; set; }

        public int MinFrequency { get; set; }

        public int MaxFrequency { get; set; }

        public SolidColorBrush IdleColor { get; set; }

        public SolidColorBrush PitchColor { get; set; }

        #endregion Properties

        #region Methods

        public override string ToString() => $"{MinFrequency} - {MaxFrequency} Hz";

        #endregion Methods
    }
}