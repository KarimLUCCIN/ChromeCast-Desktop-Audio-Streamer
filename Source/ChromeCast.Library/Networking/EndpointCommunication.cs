using ChromeCast.Desktop.AudioStreamer.ProtocolBuffer;
using ChromeCast.Library.Classes;
using ChromeCast.Library.Communication;
using ChromeCast.Library.Communication.Classes;
using ChromeCast.Library.Streaming;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChromeCast.Library.Networking
{
    public class EndpointCommunication
    {
        private readonly EndpointConnection connection;
        private string streamingUrl;
        private int requestId = 0;
        private string chromeCastDestination;
        private string chromeCastSource;
        private string chromeCastApplicationSessionNr;
        private int chromeCastMediaSessionId;

        private TimeSpan playingTime = TimeSpan.Zero;
        public event Action<EndpointCommunication, TimeSpan> PlayingTimeChanged;
        public TimeSpan PlayingTime
        {
            get { return playingTime; }
            private set
            {
                if (playingTime != value)
                {
                    playingTime = value;
                    PlayingTimeChanged?.Invoke(this, value);
                }
            }
        }

        public event Action<EndpointCommunication, DeviceState> StateChanged;
        private DeviceState deviceState = DeviceState.Idle;
        public DeviceState DeviceState
        {
            get { return deviceState; }
            set
            {
                if (deviceState != value)
                {
                    deviceState = value;
                    StateChanged?.Invoke(this, value);
                }
            }
        }

        private Volume currentVolume = new Volume() { level = 1.0f, stepInterval = .5f };
        public event Action<EndpointCommunication, Volume> VolumeChanged;
        public Volume Volume
        {
            get { return currentVolume; }
            set
            {
                if (value != currentVolume)
                {
                    currentVolume = value;
                    SetVolumeMessageAsync(value).Forget();
                }
            }
        }

        public EndpointCommunication(EndpointConnection connection)
        {
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
            chromeCastDestination = string.Empty;
            chromeCastSource = string.Format("client-8{0}", new Random().Next(10000, 99999));

            connection.StateChanged += Connection_StateChanged;
            connection.MessageReceived += Connection_MessageReceived;
        }

        private void Connection_StateChanged(EndpointConnection con, Communication.DeviceConnectionState newState)
        {
            if (newState == DeviceConnectionState.Error)
            {
                DeviceState = DeviceState.ConnectError;
            }
        }

        public async Task<bool> LaunchAsync(Action connectedCallback = null)
        {
            DeviceState = DeviceState.LaunchingApplication;
            await ConnectAsync();

            if (connection.ConnectionState == DeviceConnectionState.Connected)
            {
                await LaunchMessageAsyncM();

                chromeCastDestination = "";

                Stopwatch timeoutWait = new Stopwatch();
                timeoutWait.Start();
                while (string.IsNullOrWhiteSpace(chromeCastDestination) && timeoutWait.ElapsedMilliseconds < 5000)
                {
                    if (DeviceState == DeviceState.Closed)
                    {
                        DeviceState = DeviceState.LaunchingApplication;
                    }

                    await Task.Delay(100);
                }

                if (string.IsNullOrWhiteSpace(chromeCastDestination))
                {
                    DeviceState = DeviceState.LoadFailed;
                    return false;
                }

                DeviceState = DeviceState.LaunchedApplication;
                await ConnectAsync(chromeCastSource, chromeCastDestination, connectedCallback);

                return true;
            }
            else
            {
                return false;
            }
        }

        public int GetNextRequestId()
        {
            return ++requestId;
        }

        public async Task LaunchMessageAsyncM()
        {
            await SendMessageAsync(ChromeCastMessages.GetLaunchMessage(GetNextRequestId()));
        }

        public async Task LoadMediaMessageAsync()
        {
            DeviceState = DeviceState.LoadingMedia;
            await SendMessageAsync(ChromeCastMessages.GetLoadMessage(streamingUrl, chromeCastSource, chromeCastDestination));
        }

        public async Task PauseMediaMessageAsync()
        {
            DeviceState = DeviceState.Paused;
            await SendMessageAsync(ChromeCastMessages.GetPauseMessage(chromeCastApplicationSessionNr, chromeCastMediaSessionId, GetNextRequestId(), chromeCastSource, chromeCastDestination));
        }

        public async Task SetVolumeMessageAsync(Volume newVolume)
        {
            await SendMessageAsync(ChromeCastMessages.GetVolumeSetMessage(newVolume, GetNextRequestId()));
        }

        public async Task VolumeMuteMessageAsync(bool muted)
        {
            if (connection.ConnectionState == DeviceConnectionState.Connected)
            {
                await SendMessageAsync(ChromeCastMessages.GetVolumeMuteMessage(muted, GetNextRequestId()));
            }
        }

        public async Task PongAsync()
        {
            await SendMessageAsync(ChromeCastMessages.GetPongMessage());
        }

        public async Task GetReceiverStatusAsync()
        {
            await SendMessageAsync(ChromeCastMessages.GetReceiverStatusMessage(GetNextRequestId()));
        }

        Dictionary<int, Stopwatch> mediaStatusRequests = new Dictionary<int, Stopwatch>();

        public async Task GetMediaStatusAsync()
        {
            await SendMessageAsync(ChromeCastMessages.GetMediaStatusMessage(GetNextRequestId(), chromeCastSource, chromeCastDestination));
        }

        public async Task StopAsync()
        {
            await SendMessageAsync(ChromeCastMessages.GetStopMessage(chromeCastApplicationSessionNr, chromeCastMediaSessionId, GetNextRequestId(), chromeCastSource, chromeCastDestination));
        }

        public async Task ConnectAsync(string sourceId = null, string destinationId = null, Action connectedCallback = null)
        {
            await SendMessageAsync(ChromeCastMessages.GetConnectMessage(sourceId, destinationId));

            var listener = new StreamingRequestsListener();
            listener.Connected += async (Socket socket, string http) =>
            {
                var remoteAddress = ((IPEndPoint)socket.RemoteEndPoint).Address.ToString();
                if (remoteAddress != connection.Host)
                {
                    Debug.WriteLine("Address didn't match");
                }
                else
                {
                    await ConnectRecordingDataAsync(socket);
                }

                if (connectedCallback != null)
                {
                    connectedCallback();
                }
            };

            var endpoint = listener.StartListening(Network.GetIp4Address());
            if (endpoint == null)
            {
                throw new Exception("Can't listen ...");
            }

            streamingUrl = string.Format("http://{0}:{1}/", endpoint.Address.ToString(), endpoint.Port);
        }

        public async Task SendMessageAsync(CastMessage castMessage)
        {
            var byteMessage = ChromeCastMessages.MessageToByteArray(castMessage);
            await connection.SendMessageAsync(new ArraySegment<byte>(byteMessage));
        }

        private async void Connection_MessageReceived(EndpointConnection con, CastMessage castMessage)
        {
            var message = JsonConvert.DeserializeObject<PayloadMessageBase>(castMessage.PayloadUtf8);
            switch (message.@type)
            {
                case "RECEIVER_STATUS":
                    OnReceiveReceiverStatus(JsonConvert.DeserializeObject<MessageReceiverStatus>(castMessage.PayloadUtf8));
                    break;
                case "MEDIA_STATUS":
                    OnReceiveMediaStatus(JsonConvert.DeserializeObject<MessageMediaStatus>(castMessage.PayloadUtf8));
                    break;
                case "PING":
                    await PongAsync();
                    break;
                case "PONG":
                    var pongMessage = JsonConvert.DeserializeObject<PayloadMessageBase>(castMessage.PayloadUtf8);
                    break;
                case "CLOSE":
                    var closeMessage = JsonConvert.DeserializeObject<PayloadMessageBase>(castMessage.PayloadUtf8);
                    DeviceState = DeviceState.Closed;
                    //if (applicationLogic.GetAutoRestart())
                    //{
                    //    await Task.Delay(5000);
                    //    OnClickDeviceButton(DeviceState.Closed);
                    //}
                    break;
                case "LOAD_FAILED":
                    var loadFailedMessage = JsonConvert.DeserializeObject<MessageLoadFailed>(castMessage.PayloadUtf8);
                    DeviceState = DeviceState.LoadFailed;
                    break;
                case "LOAD_CANCELLED":
                    var loadCancelledMessage = JsonConvert.DeserializeObject<MessageLoadCancelled>(castMessage.PayloadUtf8);
                    DeviceState = DeviceState.LoadCancelled;
                    break;
                case "INVALID_REQUEST":
                    var invalidRequestMessage = JsonConvert.DeserializeObject<PayloadMessageBase>(castMessage.PayloadUtf8);
                    DeviceState = DeviceState.InvalidRequest;
                    break;
                default:
                    break;
            }
        }

        private void SetPlayingTime(MessageMediaStatus mediaStatusMessage)
        {
            if (mediaStatusMessage.status != null && mediaStatusMessage.status.First() != null)
            {
                var seconds = (int)(mediaStatusMessage.status.First().currentTime % 60);
                var minutes = ((int)(mediaStatusMessage.status.First().currentTime) % 3600) / 60;
                var hours = ((int)mediaStatusMessage.status.First().currentTime) / 3600;
                PlayingTime = new TimeSpan(hours, minutes, seconds);
            }
        }

        private void OnReceiveMediaStatus(MessageMediaStatus mediaStatusMessage)
        {
            chromeCastMediaSessionId = mediaStatusMessage.status.Any() ? mediaStatusMessage.status.First().mediaSessionId : 1;

            if (connection.ConnectionState == DeviceConnectionState.Connected && mediaStatusMessage.status.Any())
            {
                switch (mediaStatusMessage.status.First().playerState)
                {
                    case "IDLE":
                        DeviceState = DeviceState.Idle;
                        break;
                    case "BUFFERING":
                        DeviceState = DeviceState.Buffering;
                        SetPlayingTime(mediaStatusMessage);
                        break;
                    case "PAUSED":
                        DeviceState = DeviceState.Paused;
                        break;
                    case "PLAYING":
                        DeviceState = DeviceState.Playing;
                        SetPlayingTime(mediaStatusMessage);
                        break;
                    default:
                        break;
                }
            }
        }
        public async Task ConnectRecordingDataAsync(Socket socket)
        {
            if (socket == null)
            {
                throw new ArgumentNullException(nameof(socket));
            }

            await connection.ConnectRecordingDataAsync(socket);
        }

        private void OnReceiveReceiverStatus(MessageReceiverStatus receiverStatusMessage)
        {
            if (receiverStatusMessage != null && receiverStatusMessage.status != null && receiverStatusMessage.status.applications != null)
            {
                currentVolume = receiverStatusMessage.status.volume;
                VolumeChanged?.Invoke(this, currentVolume);

                var deviceApplication = receiverStatusMessage.status.applications.Where(a => a.appId.Equals("CC1AD845"));
                if (deviceApplication.Any())
                {
                    chromeCastDestination = deviceApplication.First().transportId;
                    chromeCastApplicationSessionNr = deviceApplication.First().sessionId;
                }
            }
        }
    }
}
