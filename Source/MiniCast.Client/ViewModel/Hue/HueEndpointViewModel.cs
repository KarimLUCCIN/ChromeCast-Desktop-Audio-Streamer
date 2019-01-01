using GalaSoft.MvvmLight;
using MiniCast.Hue;
using System.ComponentModel;

namespace MiniCast.Client.ViewModel.Hue
{
    public class HueEndpointViewModel : ViewModelBase, INotifyPropertyChanged
    {
        private readonly HueEndpoint deviceInfo;

        public string Id => deviceInfo.Id;
        public string Address => deviceInfo.Address;

        public HueEndpointViewModel(HueEndpoint deviceInfo)
        {
            this.deviceInfo = deviceInfo ?? throw new System.ArgumentNullException(nameof(deviceInfo));
        }
    }
}