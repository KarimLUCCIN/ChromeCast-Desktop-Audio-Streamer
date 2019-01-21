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

        public GradientSpan ColorGradient { get; } = new GradientSpan();

        public Color BaseColor { get; set; }

        private DispatcherTimer saveTimer;
        private bool colorsDirty = false;

        public MusicColorViewModel()
        {
            BaseColor = LoadColor(nameof(BaseColor), Color.FromRgb(100, 200, 0));

            LoadGradient();

            ColorGradient.GradientChanged += ColorGradient_GradientChanged;

            saveTimer = new DispatcherTimer(DispatcherPriority.Normal);
            saveTimer.Interval = TimeSpan.FromSeconds(1);
            saveTimer.Tick += SaveTimer_Tick;
            saveTimer.Start();
        }

        private void ColorGradient_GradientChanged(GradientSpan obj)
        {
            colorsDirty = true;
        }

        private void LoadGradient()
        {
            ColorGradient.Start = new GradientStop(LoadColor("Start", Color.FromRgb(0, 0, 0)), 0);
            ColorGradient.End = new GradientStop(LoadColor("End", Color.FromRgb(255, 255, 255)), 1);

            int count = CrossSettings.Current.Get<int>("MusicColor.GradientSize", 0);
            for (int i = 0; i < count; i++)
            {
                float offset = CrossSettings.Current.Get<float>($"MusicColor.Color{i}.Offset", 0);
                ColorGradient.Stops.Add(new GradientStop(LoadColor($"Color{i}", Color.FromRgb(0, 0, 0)), offset));
            }
        }

        private void SaveGradient()
        {
            SaveColor("Start", ColorGradient.Start.Color);
            SaveColor("End", ColorGradient.End.Color);

            var interior = ColorGradient.InteriorStops.ToArray();
            CrossSettings.Current.Set("MusicColor.GradientSize", interior.Length);
            for (int i = 0; i < interior.Length; i++)
            {
                var stop = interior[i];
                CrossSettings.Current.Set($"MusicColor.Color{i}.Offset", stop.Offset);
                SaveColor($"Color{i}", stop.Color);
            }
        }

        private void SaveTimer_Tick(object sender, EventArgs e)
        {
            if (colorsDirty)
            {
                colorsDirty = false;

                SaveColor(nameof(BaseColor), BaseColor);
                SaveGradient();
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
