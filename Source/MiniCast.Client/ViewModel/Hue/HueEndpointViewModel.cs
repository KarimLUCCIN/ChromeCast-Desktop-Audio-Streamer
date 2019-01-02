using Acr.Settings;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using MiniCast.Hue;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MiniCast.Client.ViewModel.Hue
{
    public class HueEndpointViewModel : ViewModelBase, INotifyPropertyChanged
    {
        private struct EndpointSettings
        {
            public string AppKey;
        }

        private EndpointSettings localSettings;
        private readonly HueEndpoint deviceInfo;

        public string Id => deviceInfo.Id;
        public string Address => deviceInfo.Address;
        private string SettingsKey => "Hue.Endpoint." + Id;

        public bool IsConnected { get; private set; } = false;
        public bool IsNotConnected => !IsConnected;
        public bool IsBusy { get; private set; } = false;
        public bool CanConnect => !IsConnected && !IsBusy;

        public string ConnectErrorMessage { get; private set; }
        public bool HasConnectErrorMessage => !string.IsNullOrWhiteSpace(ConnectErrorMessage);

        public RelayCommand ConnectCommand { get; private set; }

        public HueEndpointViewModel(HueEndpoint deviceInfo)
        {
            this.deviceInfo = deviceInfo ?? throw new System.ArgumentNullException(nameof(deviceInfo));

            ConnectCommand = new RelayCommand(async () => await ConnectAsync(autoRegister: true));

            localSettings = CrossSettings.Current.Get<EndpointSettings>(SettingsKey);
            if (!string.IsNullOrEmpty(localSettings.AppKey))
            {
                ConnectAsync(autoRegister: false);
            }
        }

        private void CommitSettings()
        {
            CrossSettings.Current.Set(SettingsKey, localSettings);
        }

        public async Task ConnectAsync(bool autoRegister)
        {
            if (IsBusy)
            {
                return;
            }
            IsBusy = true;

            try
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(localSettings.AppKey))
                    {
                        try
                        {
                            deviceInfo.Client.Initialize(localSettings.AppKey);
                        }
                        catch
                        {
                            await RegisterInternalAsync();
                        }
                    }
                    else
                    {
                        await RegisterInternalAsync();
                    }

                    ConnectErrorMessage = string.Empty;
                    IsConnected = true;
                }
                catch (Exception ex)
                {
                    ConnectErrorMessage = ex.Message;
                    Debug.WriteLine(ex.ToString());
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RegisterInternalAsync()
        {
            localSettings.AppKey = await deviceInfo.Client.RegisterAsync("MiniCast.Endpoint", Environment.MachineName);
            CommitSettings();
        }
    }
}