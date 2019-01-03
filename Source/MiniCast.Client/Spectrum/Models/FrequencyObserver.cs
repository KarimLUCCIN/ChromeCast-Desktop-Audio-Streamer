using System.ComponentModel;

namespace SpectrumAnalyzer.Models
{
    public class FrequencyObserver : FrequencyBin, INotifyPropertyChanged
    {
        #region Properties

        public double AverageFactor { get; set; } = 1.2;

        public double AverageEnergy { get; set; }

        public double AverageEnergyPercentage { get; set; } = 30;

        public double AverageEnergyAdjustment { get; set; } = 0.001;

        public double AverageEnergyThreshold => AverageEnergy * AverageEnergyPercentage * 0.01;

        public string Title { get; set; }

        public bool BeatDetected { get; set; }

        #endregion Properties

        #region Public Methods

        public void AdjustAverage(float cur)
        {
            AverageEnergy = AverageEnergy * (1 - AverageEnergyAdjustment) + cur * AverageEnergyAdjustment;
        }

        #endregion Public Methods
    }
}