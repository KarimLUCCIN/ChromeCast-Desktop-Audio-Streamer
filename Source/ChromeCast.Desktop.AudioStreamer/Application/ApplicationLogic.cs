using System;
using System.Drawing;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;
using Rssdp;
using NAudio.Wave;
using ChromeCast.Desktop.AudioStreamer.Classes;
using System.Net;
using ChromeCast.Desktop.AudioStreamer.Streaming;
using ChromeCast.Desktop.AudioStreamer.Discover;

namespace ChromeCast.Desktop.AudioStreamer.Application
{
    public class ApplicationLogic
    {
        private Devices devices;
        private MainForm mainForm;
        private Configuration configuration;
        private LoopbackRecorder loopbackRecorder;
        private StreamingRequestsListener streamingRequestListener;
        private DiscoverDevices discoverDevices;
        private DeviceStatusTimer deviceStatusTimer;
        private NotifyIcon notifyIcon;
        private const int trbLagMaximumValue = 1000;
        private string streamingUrl = string.Empty;
        private bool playingOnIpChange;

        private bool AutoRestart { get; set; } = false;

        public ApplicationLogic(Devices devicesIn, DiscoverDevices discoverDevicesIn
            , LoopbackRecorder loopbackRecorderIn, Configuration configurationIn
            , StreamingRequestsListener streamingRequestListenerIn, DeviceStatusTimer deviceStatusTimerIn)
        {
            devices = devicesIn;
            devices.SetCallback(OnAddDevice);
            discoverDevices = discoverDevicesIn;
            loopbackRecorder = loopbackRecorderIn;
            configuration = configurationIn;
            streamingRequestListener = streamingRequestListenerIn;
            deviceStatusTimer = deviceStatusTimerIn;
        }

        public void Start()
        {
            var ipAddress = Network.GetIp4Address();
            Task.Run(() => { streamingRequestListener.StartListening(ipAddress, OnStreamingRequestsListen, OnStreamingRequestConnect); });
            AddNotifyIcon();
            configuration.Load(SetConfiguration);
            ScanForDevices();
            deviceStatusTimer.StartPollingDevice(devices.OnGetStatus);
            loopbackRecorder.GetDevices(mainForm);
        }

        private void ToggleFormVisibility(object sender, EventArgs e)
        {
            if (e.GetType().Equals(typeof(MouseEventArgs)))
            {
                if (((MouseEventArgs)e).Button != MouseButtons.Left) return;
            }

            mainForm.ToggleVisibility();
        }

        public void OnStreamingRequestsListen(string host, int port)
        {
            Console.WriteLine(string.Format("Streaming from {0}:{1}", host, port));
            streamingUrl = string.Format("http://{0}:{1}/", host, port);

            devices.SetStreamingUrl(streamingUrl);
        }

        public void OnStreamingRequestConnect(Socket socket, string httpRequest)
        {
            Console.WriteLine(string.Format("Connection added from {0}", socket.RemoteEndPoint));

            loopbackRecorder.StartRecording((ArraySegment<byte> dataToSend, WaveFormat format) => {
                OnRecordingDataAvailable(dataToSend, format);
            });
            devices.AddStreamingConnection(socket, httpRequest);
        }

        public void OnRecordingDataAvailable(ArraySegment<byte> dataToSend, WaveFormat format)
        {
            devices.OnRecordingDataAvailable(dataToSend, format);
        }

        public void OnSetHooks(bool setHooks)
        {
            if (setHooks)
                SetWindowsHook.Start(devices);
            else
                SetWindowsHook.Stop();
        }

        public void OnAddDevice(Device device)
        {
            var menuItem = new MenuItem();
            menuItem.Text = device.GetFriendlyName();
            menuItem.Click += device.OnClickDeviceButton;
            notifyIcon.ContextMenu.MenuItems.Add(notifyIcon.ContextMenu.MenuItems.Count - 1, menuItem);
            device.SetMenuItem(menuItem);

            mainForm.AddDevice(device);
        }

        private void AddNotifyIcon()
        {
            var contextMenu = new ContextMenu();
            var menuItem = new MenuItem();
            menuItem.Index = 0;
            menuItem.Text = "Close";
            menuItem.Click += new EventHandler(CloseApplication);
            contextMenu.MenuItems.AddRange(new MenuItem[] { menuItem });

            notifyIcon = new NotifyIcon();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            notifyIcon.Icon = ((Icon)(resources.GetObject("$this.Icon")));
            notifyIcon.Visible = true;
            notifyIcon.Text = "ChromeCast Desktop Streamer";
            notifyIcon.ContextMenu = contextMenu;
            notifyIcon.Click += ToggleFormVisibility;
        }

        public string GetStreamingUrl()
        {
            return streamingUrl;
        }

        public void SetLagThreshold(int lagThreshold)
        {
        }

        public void SetConfiguration(bool useShortCuts, bool showLog, bool showLagControl, int lagValue, bool autoStart, string ipAddressesDevices, bool showWindow, bool autoRestart)
        {
            mainForm.SetKeyboardHooks(useShortCuts);
            mainForm.ShowLog(showLog);
            mainForm.ShowLagControl(showLagControl);
            mainForm.SetLagValue(lagValue);
            devices.SetAutoStart(autoStart);
            mainForm.SetWindowVisibility(showWindow);
            mainForm.SetAutoRestart(autoRestart);

            if (!string.IsNullOrWhiteSpace(ipAddressesDevices))
            {
                var ipDevices = ipAddressesDevices.Split(';');
                foreach (var ipDevice in ipDevices)
                {
                    var arrDevice = ipDevice.Split(',');
                    devices.OnDeviceAvailable(
                            new DiscoveredSsdpDevice { DescriptionLocation = new Uri($"http://{arrDevice[0]}") },
                            new SsdpRootDevice { FriendlyName = arrDevice[1] }
                        );
                }
            }
        }

        public void CloseApplication()
        {
            SetWindowsHook.Stop();
            loopbackRecorder.StopRecording();
            devices.Dispose();
            streamingRequestListener.StopListening();
            notifyIcon.Visible = false;
            mainForm.Dispose();
        }

        private void CloseApplication(object sender, EventArgs e)
        {
            CloseApplication();
        }

        public void SetDependencies(MainForm mainFormIn)
        {
            mainForm = mainFormIn;
        }

        public void RecordingDeviceChanged()
        {
            loopbackRecorder.StartRecordingDevice();
        }

        public void OnSetAutoRestart(bool autoRestart)
        {
            AutoRestart = autoRestart;
        }

        public bool GetAutoRestart()
        {
            if (playingOnIpChange)
            {
                playingOnIpChange = false;
                return true;
            }
            else
            {
                return AutoRestart;
            }
        }

        public async void ChangeIPAddressUsed(IPAddress ipAddress)
        {
            playingOnIpChange = devices.Stop();
            streamingRequestListener.StopListening();
            await Task.Run(() => { streamingRequestListener.StartListening(ipAddress, OnStreamingRequestsListen, OnStreamingRequestConnect); });
            if (playingOnIpChange)
            {
                await Task.Delay(2500);
                devices.Start();
                await Task.Delay(15000);
                playingOnIpChange = false;
            }
        }

        public void ScanForDevices()
        {
            discoverDevices.Discover(devices.OnDeviceAvailable);
        }
    }
}
