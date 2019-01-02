using Acr.Settings;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using MiniCast.Hue;
using Q42.HueApi.ColorConverters;
using Q42.HueApi.Streaming;
using Q42.HueApi.Streaming.Extensions;
using Q42.HueApi.Streaming.Models;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiniCast.Client.ViewModel.Hue
{
    public class HueEndpointViewModel : ViewModelBase, INotifyPropertyChanged
    {
        private struct EndpointSettings
        {
            public string AppKey;
            public string StreamingKey;
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

        public string ErrorMessage { get; private set; }
        public bool HasErrorMessage => !string.IsNullOrWhiteSpace(ErrorMessage);

        public string Name { get; private set; }

        public RelayCommand ConnectCommand { get; private set; }
        public RelayCommand TestCommand { get; private set; }

        private CancellationTokenSource globalEffectsCancelSource = new CancellationTokenSource();
        private EntertainmentLayer entertainmentLayer = null;

        public HueEndpointViewModel(HueEndpoint deviceInfo)
        {
            this.deviceInfo = deviceInfo ?? throw new System.ArgumentNullException(nameof(deviceInfo));

            ConnectCommand = new RelayCommand(async () => await ConnectAsync(autoRegister: true));
            TestCommand = new RelayCommand(async () => await TestAsync());

            localSettings = CrossSettings.Current.Get<EndpointSettings>(SettingsKey);
            if (!string.IsNullOrEmpty(localSettings.AppKey))
            {
                ConnectAsync(autoRegister: false).Forget();
            }
        }

        public override void Cleanup()
        {
            globalEffectsCancelSource.Cancel();

            base.Cleanup();
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
                    if (!string.IsNullOrWhiteSpace(localSettings.AppKey) && !string.IsNullOrWhiteSpace(localSettings.StreamingKey))
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

                    ErrorMessage = string.Empty;

                    await LoadBridgeInfoAsync();

                    IsConnected = true;
                }
                catch (Exception ex)
                {
                    ErrorMessage = ex.Message;
                    Debug.WriteLine(ex.ToString());
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadBridgeInfoAsync()
        {
            var bridge = await deviceInfo.Client.GetBridgeAsync();

            Name = bridge.Config.Name;
        }

        private async Task<EntertainmentLayer> GetOrCreateEntertainmentLayerAsync()
        {
            if (entertainmentLayer != null)
            {
                return entertainmentLayer;
            }

            var group = (await deviceInfo.Client.GetEntertainmentGroups()).FirstOrDefault();
            if (group == null)
            {
                ErrorMessage = "No default entertainment group";
                return null;
            }

            Debug.WriteLine($"Group: {group.Name}");

            var streamClient = new StreamingHueClient(deviceInfo.Address, localSettings.AppKey, localSettings.StreamingKey);
            var stream = new StreamingGroup(group.Lights);

            await streamClient.Connect(group.Id);

            streamClient.AutoUpdate(stream, globalEffectsCancelSource.Token);

            return entertainmentLayer = stream.GetNewLayer(isBaseLayer: true);
        }

        public async Task TestAsync()
        {
            var layer = await GetOrCreateEntertainmentLayerAsync();

            if (layer == null)
            {
                return;
            }

            for (int i = 0; i < 4; i++)
            {
                layer.SetState(CancellationToken.None, RandomColor(), .5, TimeSpan.FromSeconds(.5));

                await Task.Delay(TimeSpan.FromSeconds(1));

                layer.SetState(CancellationToken.None, RandomColor(), 1, TimeSpan.FromSeconds(.5));

                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        private static RGBColor RandomColor()
        {
            var r = new Random(DateTime.Now.Millisecond);
            return new RGBColor(r.NextDouble(), r.NextDouble(), r.NextDouble());
        }

        private async Task RegisterInternalAsync()
        {
            var registerResult = await deviceInfo.Client.RegisterAsync("MiniCast.Endpoint", Environment.MachineName, generateClientKey: true);
            localSettings.AppKey = registerResult.Username;
            localSettings.StreamingKey = registerResult.StreamingClientKey;
            CommitSettings();
        }
    }
}