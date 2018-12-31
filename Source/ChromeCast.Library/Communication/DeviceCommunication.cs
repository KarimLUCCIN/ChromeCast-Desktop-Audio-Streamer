﻿using System;
using System.Linq;
using ChromeCast.Library.Communication.Classes;
using System.Threading.Tasks;
using ChromeCast.Library.Application;
using ChromeCast.Desktop.AudioStreamer.ProtocolBuffer;
using Newtonsoft.Json;

namespace ChromeCast.Library.Communication
{
    public class DeviceCommunication
    {
        private Action<DeviceState, string> setDeviceState;
        private Action<Volume> onVolumeUpdate;
        private Action<byte[]> sendMessage;
        private Func<bool> isConnected;
        private Func<bool> isDeviceConnected;
        private Func<string> getHost;
        private Func<DeviceState> getDeviceState;
        private Logger logger;
        private ChromeCastMessages chromeCastMessages;
        private string chromeCastDestination;
        private string chromeCastSource;
        private string chromeCastApplicationSessionNr;
        private int chromeCastMediaSessionId;
        private int requestId;
        private VolumeSetItem lastVolumeSetItem;
        private VolumeSetItem nextVolumeSetItem;

        public DeviceCommunication(Logger loggerIn, ChromeCastMessages chromeCastMessagesIn)
        {
            logger = loggerIn;
            chromeCastMessages = chromeCastMessagesIn;
            chromeCastDestination = string.Empty;
            chromeCastSource = string.Format("client-8{0}", new Random().Next(10000, 99999));
            requestId = 0;
        }

        public void LaunchAndLoadMedia()
        {
            setDeviceState?.Invoke(DeviceState.LaunchingApplication, null);
            Connect();
            if (isDeviceConnected())
                Launch();
        }

        public void Connect(string sourceId = null, string destinationId = null)
        {
            SendMessage(chromeCastMessages.GetConnectMessage(sourceId, destinationId));
        }

        public void Launch()
        {
            SendMessage(chromeCastMessages.GetLaunchMessage(GetNextRequestId()));
        }

        public void LoadMedia(string streamingUrl)
        {
            setDeviceState?.Invoke(DeviceState.LoadingMedia, null);
            SendMessage(chromeCastMessages.GetLoadMessage(streamingUrl, chromeCastSource, chromeCastDestination));
        }

        public void PauseMedia()
        {
            setDeviceState?.Invoke(DeviceState.Paused, null);
            SendMessage(chromeCastMessages.GetPauseMessage(chromeCastApplicationSessionNr, chromeCastMediaSessionId, GetNextRequestId(), chromeCastSource, chromeCastDestination));
        }

        public void VolumeSet(Volume volumeSetting)
        {
            if (isConnected())
            {
                nextVolumeSetItem = new VolumeSetItem { Setting = volumeSetting };
                SendVolumeSet();
            }
        }

        private void SendVolumeSet()
        {
            if ((nextVolumeSetItem != null && lastVolumeSetItem == null)
                || (lastVolumeSetItem != null && DateTime.Now.Subtract(lastVolumeSetItem.SendAt) > new TimeSpan(0, 0, 1)))
            {
                lastVolumeSetItem = nextVolumeSetItem;
                lastVolumeSetItem.RequestId = GetNextRequestId();
                lastVolumeSetItem.SendAt = DateTime.Now;
                SendMessage(chromeCastMessages.GetVolumeSetMessage(lastVolumeSetItem.Setting, lastVolumeSetItem.RequestId));
                nextVolumeSetItem = null;
            }
        }

        public void VolumeMute(bool muted)
        {
            if (isConnected())
                SendMessage(chromeCastMessages.GetVolumeMuteMessage(muted, GetNextRequestId()));
        }

        public void Pong()
        {
            SendMessage(chromeCastMessages.GetPongMessage());
        }

        public void GetReceiverStatus()
        {
            SendMessage(chromeCastMessages.GetReceiverStatusMessage(GetNextRequestId()));
        }

        public void GetMediaStatus()
        {
            SendMessage(chromeCastMessages.GetMediaStatusMessage(GetNextRequestId(), chromeCastSource, chromeCastDestination));
        }

        public void Stop()
        {
            SendMessage(chromeCastMessages.GetStopMessage(chromeCastApplicationSessionNr, chromeCastMediaSessionId, GetNextRequestId(), chromeCastSource, chromeCastDestination));
        }

        public int GetNextRequestId()
        {
            return ++requestId;
        }

        public void SendMessage(CastMessage castMessage)
        {
            var byteMessage = chromeCastMessages.MessageToByteArray(castMessage);
            sendMessage?.Invoke(byteMessage);

            logger.Log(string.Format("out [{2}][{0}]: {1}", getHost?.Invoke(), castMessage.PayloadUtf8, DateTime.Now.ToLongTimeString()));
        }

        public void OnReceiveMessage(CastMessage castMessage, string streamingUrl)
        {
            logger.Log(string.Format("in [{2}] [{0}]: {1}", getHost?.Invoke(), castMessage.PayloadUtf8, DateTime.Now.ToLongTimeString()));

            var message = JsonConvert.DeserializeObject<PayloadMessageBase>(castMessage.PayloadUtf8);
            switch (message.@type)
            {
                case "RECEIVER_STATUS":
                    OnReceiveReceiverStatus(JsonConvert.DeserializeObject<MessageReceiverStatus>(castMessage.PayloadUtf8), streamingUrl);
                    break;
                case "MEDIA_STATUS":
                    OnReceiveMediaStatus(JsonConvert.DeserializeObject<MessageMediaStatus>(castMessage.PayloadUtf8));
                    break;
                case "PING":
                    Pong();
                    break;
                case "PONG":
                    var pongMessage = JsonConvert.DeserializeObject<PayloadMessageBase>(castMessage.PayloadUtf8);
                    break;
                case "CLOSE":
                    var closeMessage = JsonConvert.DeserializeObject<PayloadMessageBase>(castMessage.PayloadUtf8);
                    setDeviceState(DeviceState.Closed, null);
                    //if (applicationLogic.GetAutoRestart())
                    //{
                    //    await Task.Delay(5000);
                    //    OnClickDeviceButton(DeviceState.Closed);
                    //}
                    break;
                case "LOAD_FAILED":
                    var loadFailedMessage = JsonConvert.DeserializeObject<MessageLoadFailed>(castMessage.PayloadUtf8);
                    setDeviceState(DeviceState.LoadFailed, null);
                    break;
                case "LOAD_CANCELLED":
                    var loadCancelledMessage = JsonConvert.DeserializeObject<MessageLoadCancelled>(castMessage.PayloadUtf8);
                    setDeviceState(DeviceState.LoadCancelled, null);
                    break;
                case "INVALID_REQUEST":
                    var invalidRequestMessage = JsonConvert.DeserializeObject<PayloadMessageBase>(castMessage.PayloadUtf8);
                    setDeviceState(DeviceState.InvalidRequest, null);
                    break;
                default:
                    break;
            }
        }

        private void OnReceiveMediaStatus(MessageMediaStatus mediaStatusMessage)
        {
            chromeCastMediaSessionId = mediaStatusMessage.status.Any() ? mediaStatusMessage.status.First().mediaSessionId : 1;

            if (isConnected() && mediaStatusMessage.status.Any())
            {
                switch (mediaStatusMessage.status.First().playerState)
                {
                    case "IDLE":
                        setDeviceState(DeviceState.Idle, null);
                        break;
                    case "BUFFERING":
                        setDeviceState(DeviceState.Buffering, GetPlayingTime(mediaStatusMessage));
                        break;
                    case "PAUSED":
                        setDeviceState(DeviceState.Paused, null);
                        break;
                    case "PLAYING":
                        setDeviceState(DeviceState.Playing, GetPlayingTime(mediaStatusMessage));
                        break;
                    default:
                        break;
                }
            }
        }

        private string GetPlayingTime(MessageMediaStatus mediaStatusMessage)
        {
            if (mediaStatusMessage.status != null && mediaStatusMessage.status.First() != null)
            {
                var seconds = (int)(mediaStatusMessage.status.First().currentTime % 60);
                var minutes = ((int)(mediaStatusMessage.status.First().currentTime) % 3600) / 60;
                var hours = ((int)mediaStatusMessage.status.First().currentTime) / 3600;
                return string.Format("{0}:{1}:{2}", hours, minutes.ToString("D2"), seconds.ToString("D2"));
            }

            return null;
        }

        private void OnReceiveReceiverStatus(MessageReceiverStatus receiverStatusMessage, string streamingUrl)
        {
            if (receiverStatusMessage != null && receiverStatusMessage.status != null && receiverStatusMessage.status.applications != null)
            {
                onVolumeUpdate(receiverStatusMessage.status.volume);

                var deviceApplication = receiverStatusMessage.status.applications.Where(a => a.appId.Equals("CC1AD845"));
                if (deviceApplication.Any())
                {
                    chromeCastDestination = deviceApplication.First().transportId;
                    chromeCastApplicationSessionNr = deviceApplication.First().sessionId;

                    if (getDeviceState().Equals(DeviceState.LaunchingApplication))
                    {
                        setDeviceState(DeviceState.LaunchedApplication, null);
                        Connect(chromeCastSource, chromeCastDestination);
                        LoadMedia(streamingUrl);
                    }
                }
            }

            if (lastVolumeSetItem != null && lastVolumeSetItem.RequestId == receiverStatusMessage.requestId)
            {
                lastVolumeSetItem = null;
                SendVolumeSet();
            }
        }

        public void SetCallback(Action<DeviceState, string> setDeviceStateIn, Action<Volume> onVolumeUpdateIn, Action<byte[]> sendMessageIn, 
            Func<DeviceState> getDeviceStateIn, Func<bool> isConnectedIn, Func<bool> isDeviceConnectedIn, Func<string> getHostIn)
        {
            setDeviceState = setDeviceStateIn;
            onVolumeUpdate = onVolumeUpdateIn;
            sendMessage = sendMessageIn;
            getDeviceState = getDeviceStateIn;
            isConnected = isConnectedIn;
            isDeviceConnected = isDeviceConnectedIn;
            getHost = getHostIn;
        }

        public void OnClickDeviceButton(DeviceState deviceState, string streamingUrl)
        {
            switch (deviceState)
            {
                case DeviceState.Buffering:
                case DeviceState.Playing:
                    PauseMedia();
                    break;
                case DeviceState.LaunchingApplication:
                case DeviceState.LaunchedApplication:
                case DeviceState.LoadingMedia:
                case DeviceState.Idle:
                case DeviceState.Paused:
                    LoadMedia(streamingUrl);
                    break;
                case DeviceState.NotConnected:
                case DeviceState.ConnectError:
                case DeviceState.Closed:
                case DeviceState.LoadCancelled:
                case DeviceState.LoadFailed:
                case DeviceState.InvalidRequest:
                    LaunchAndLoadMedia();
                    break;
                case DeviceState.Disposed:
                    break;
                default:
                    break;
            }
        }
    }

    public class VolumeSetItem
    {
        public Volume Setting { get; set; }
        public int RequestId { get; set; }
        public DateTime SendAt { get; set; }
    }
}
