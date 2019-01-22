using GalaSoft.MvvmLight;
using MiniCast.Client.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace MiniCast.Client.ViewModel.Hue
{
    public class HueViewModel : RootViewModelBase, INotifyPropertyChanged
    {
        public HueDevicesEnumeratorViewModel DevicesEnumeratorViewModel { get; } = new HueDevicesEnumeratorViewModel();

        public bool Updating { get; private set; }

        private DeferredStream<Color> deferredStreamColor = new DeferredStream<Color>((a, b, amount) => ColorHelpers.Lerp(a, b, (float)amount));

        public TimeSpan AudioDelay
        {
            get { return deferredStreamColor.Delay; }
            set { deferredStreamColor.Delay = value; }
        }

        public HueViewModel()
        {
            DevicesEnumeratorViewModel.ScanForDevicesCommand.Execute(null);
        }

        public override void Cleanup()
        {
            DevicesEnumeratorViewModel.Cleanup();

            base.Cleanup();
        }

        private static float GammaCorrect(float value)
        {
            return (float)((value > 0.04045f) ? Math.Pow((value + 0.055f) / (1.0f + 0.055f), 2.4f) : (value / 12.92f));
        }

        private static Color GammaCorrect(Color c)
        {
            return new Color()
            {
                ScR = GammaCorrect(c.ScR),
                ScG = GammaCorrect(c.ScG),
                ScB = GammaCorrect(c.ScB),
                ScA = GammaCorrect(c.ScA)
            };
        }

        public async void BeginUpdateEveryFrame()
        {
            if (Updating)
            {
                return;
            }
            Updating = true;

            var liveColorVm = ViewModelLocator.Instance.LiveColor;
            var chromecastDevicesVm = ViewModelLocator.Instance.Chromecast;


            ulong lastColorVersion = ulong.MaxValue;

            while (true)
            {
                await Task.Delay(1);

                if (lastColorVersion != liveColorVm.CurrentColorVersion)
                {
                    var delayInstant = (chromecastDevicesVm.CurrentDevice?.AudioDelay ?? AudioDelay);

                    AudioDelay = TimeSpan.FromSeconds(.95 * AudioDelay.TotalSeconds + 0.05 * delayInstant.TotalSeconds);
                    Debug.WriteLine($"Delay: {AudioDelay.TotalSeconds}");

                    deferredStreamColor.Add(GammaCorrect(liveColorVm.CurrentColor));

                    var currentColor = deferredStreamColor.GetCurrent();

                    foreach (var device in DevicesEnumeratorViewModel.KnownDevices)
                    {
                        device.Update(currentColor);
                    }

                    lastColorVersion = liveColorVm.CurrentColorVersion;
                }
            }
        }
    }
}
