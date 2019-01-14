using Acr.Settings;
using ColorWheel.Core;
using GalaSoft.MvvmLight;
using MiniCast.Client.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;

namespace MiniCast.Client.ViewModel
{
    public class MusicColorViewModel : RootViewModelBase, INotifyPropertyChanged
    {
        public override bool HasGlobalSpectrum => false;

        public Palette ColorPalette { get; } = Palette.Create(new RGBColorWheel(), Colors.BlueViolet, PaletteSchemaType.Custom, 3);
        public GradientSpan ColorGradient { get; } = new GradientSpan();

        public Color BaseColor => ColorPalette.Colors[0].RgbColor;
        public Color HighOctavesColor => ColorPalette.Colors[1].RgbColor;
        public Color LowOctavesColor => ColorPalette.Colors[2].RgbColor;

        private DispatcherTimer saveTimer;
        private bool colorsDirty = false;

        public MusicColorViewModel()
        {
            ColorGradient.Stops.Add(new GradientStop() { Color = Color.FromRgb(255, 0, 0), Offset = .2 });
            ColorGradient.Stops.Add(new GradientStop() { Color = Color.FromRgb(255, 255, 0), Offset = .4 });
            ColorGradient.Stops.Add(new GradientStop() { Color = Color.FromRgb(255, 0, 255), Offset = .7 });

            ColorPalette.Colors[0].Name = "Base Color";
            ColorPalette.Colors[0].RgbColor = LoadColor(nameof(BaseColor), BaseColor);
            ColorPalette.Colors[0].PropertyChanged += (_, __) => RaisePropertyChanged(nameof(BaseColor));

            ColorPalette.Colors[1].Name = "High Octaves";
            ColorPalette.Colors[1].RgbColor = LoadColor(nameof(HighOctavesColor), HighOctavesColor);
            ColorPalette.Colors[1].PropertyChanged += (_, __) => RaisePropertyChanged(nameof(HighOctavesColor));

            ColorPalette.Colors[2].Name = "Low Octaves";
            ColorPalette.Colors[2].RgbColor = LoadColor(nameof(LowOctavesColor), LowOctavesColor);
            ColorPalette.Colors[2].PropertyChanged += (_, __) => RaisePropertyChanged(nameof(LowOctavesColor));

            saveTimer = new DispatcherTimer(DispatcherPriority.Normal);
            saveTimer.Interval = TimeSpan.FromSeconds(1);
            saveTimer.Tick += SaveTimer_Tick;
            saveTimer.Start();
        }

        private void SaveTimer_Tick(object sender, EventArgs e)
        {
            if (colorsDirty)
            {
                colorsDirty = false;

                SaveColor(nameof(BaseColor), BaseColor);
                SaveColor(nameof(HighOctavesColor), HighOctavesColor);
                SaveColor(nameof(LowOctavesColor), LowOctavesColor);
            }
        }

        private void SaveColor(string name, Color value)
        {
            CrossSettings.Current.Set("MusicColor." + name, new Vector4(value.ScR, value.ScG, value.ScB, value.ScA));
        }

        private Color LoadColor(string name, Color defaultValue)
        {
            var value = CrossSettings.Current.Get("MusicColor." + name, new Vector4(defaultValue.ScR, defaultValue.ScG, defaultValue.ScB, defaultValue.ScA));
            return new Color() { ScR = value.X, ScG = value.Y, ScB = value.Z, ScA = value.W };
        }

        public override void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            colorsDirty = true;

            base.RaisePropertyChanged(propertyName);
        }
    }
}
