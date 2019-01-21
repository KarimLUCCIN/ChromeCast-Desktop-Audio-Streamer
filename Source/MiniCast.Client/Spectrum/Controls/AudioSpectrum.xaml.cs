using MiniCast.Client.Spectrum.Helpers;
using SpectrumAnalyzer.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SpectrumAnalyzer.Controls
{
    /// <summary>
    ///     Interaktionslogik für AudioSpectrum.xaml
    /// </summary>
    public partial class AudioSpectrum
    {
        public AudioSpectrum()
        {
            InitializeComponent();
            SizeChanged += AdjustLines;
        }

        private void AdjustLines(object sender, SizeChangedEventArgs _)
        {
            var items = 
                (
                    from object item in Spectrum.Items
                    let bin = item as FrequencyBin
                    where bin != null
                    let container = UIHelpers.FindVisualChildren<AudioLine>(Spectrum.ItemContainerGenerator.ContainerFromItem(bin)).FirstOrDefault()
                    where container != null
                    select container
                ).ToArray();
            var margin = items.FirstOrDefault()?.Margin;
            var offset = margin?.Top + margin?.Bottom ?? 0;

            double width = 5;

            double widthOffset = ((margin?.Right ?? 0) + (margin?.Left ?? 0));

            if (items.Length > 0)
            {
                width = Math.Max(5, ActualWidth / items.Length - widthOffset);
            }

            foreach (var spectrumItem in items)
            {
                spectrumItem.Height = ActualHeight - offset;
                spectrumItem.Width = width;
            };
        }

        #region Dependency Properties

        public static readonly DependencyProperty SpeedDroppingProperty = DependencyProperty.Register(
            "SpeedDropping", typeof(double), typeof(AudioSpectrum), new PropertyMetadata(25.5d));

        public double SpeedDropping
        {
            get => (double)GetValue(SpeedDroppingProperty);
            set => SetValue(SpeedDroppingProperty, value);
        }

        public static readonly DependencyProperty SpeedRaisingProperty = DependencyProperty.Register(
            "SpeedRaising", typeof(double), typeof(AudioSpectrum), new PropertyMetadata(25.5d));

        public double SpeedRaising
        {
            get => (double)GetValue(SpeedRaisingProperty);
            set => SetValue(SpeedRaisingProperty, value);
        }

        public new static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register(
            "Foreground", typeof(SolidColorBrush), typeof(AudioSpectrum), new PropertyMetadata(Brushes.DimGray));

        public new SolidColorBrush Foreground
        {
            get => (SolidColorBrush)GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public static readonly DependencyProperty ForegroundPitchedProperty = DependencyProperty.Register(
            "ForegroundPitched", typeof(SolidColorBrush), typeof(AudioSpectrum), new PropertyMetadata(Brushes.DarkRed));

        public SolidColorBrush ForegroundPitched
        {
            get => (SolidColorBrush)GetValue(ForegroundPitchedProperty);
            set => SetValue(ForegroundPitchedProperty, value);
        }

        public static readonly DependencyProperty PitchColorProperty = DependencyProperty.Register(
            "PitchColor", typeof(bool), typeof(AudioSpectrum), new PropertyMetadata(default(bool)));

        public bool PitchColor
        {
            get => (bool)GetValue(PitchColorProperty);
            set => SetValue(PitchColorProperty, value);
        }

        #endregion
    }
}