using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
            ulong lastColorVersion = ulong.MaxValue;

            while (true)
            {
                await Task.Delay(1);

                if (lastColorVersion != liveColorVm.CurrentColorVersion)
                {
                    var currentColor = GammaCorrect(liveColorVm.CurrentColor);

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
