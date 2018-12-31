﻿using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using ChromeCast.Library.Communication.Classes;
using ChromeCast.Desktop.AudioStreamer.ProtocolBuffer;
using Newtonsoft.Json;

namespace ChromeCast.Library.Communication
{
    public class ChromeCastMessages
    {
        private const string namespaceConnect = "urn:x-cast:com.google.cast.tp.connection";
        private const string namespaceHeartbeat = "urn:x-cast:com.google.cast.tp.heartbeat";
        private const string namespaceReceiver = "urn:x-cast:com.google.cast.receiver";
        private const string namespaceMedia = "urn:x-cast:com.google.cast.media";

        public CastMessage GetVolumeSetMessage(Volume volume, int requestId, string sourceId = null, string destinationId = null)
        {
            var volumeMessage = new MessageVolume
            {
                type = "SET_VOLUME",
                volume = new SendVolume
                {
                    level = volume.level
                },
                requestId = requestId
            };
            return GetCastMessage(volumeMessage, namespaceReceiver, sourceId, destinationId);
        }

        public CastMessage GetVolumeMuteMessage(bool muted, int requestId, string sourceId = null, string destinationId = null)
        {
            var volumeMessage = new MessageVolumeMute
            {
                type = "SET_VOLUME",
                volume = new SendVolumeMute
                {
                    muted = muted
                },
                requestId = requestId
            };
            return GetCastMessage(volumeMessage, namespaceReceiver, sourceId, destinationId);
        }

        public CastMessage GetConnectMessage(string sourceId = null, string destinationId = null)
        {
            return GetCastMessage(new PayloadMessageBase { type = "CONNECT" }, namespaceConnect, sourceId, destinationId);
        }

        public CastMessage GetLaunchMessage(int requestId)
        {
            var message = new MessageLaunch { type = "LAUNCH", appId = "CC1AD845", requestId = requestId };
            return GetCastMessage(message, namespaceReceiver);
        }

        public CastMessage GetLoadMessage(string streamingUrl, string sourceId, string destinationId)
        {
            var message = new MessageLoad
            {
                type = "LOAD",
                autoplay = true,
                currentTime = 0,
                activeTrackIds = new List<object>(),
                repeatMode = "REPEAT_OFF",
                media = new Media
                {
                    contentId = streamingUrl,
                    contentType = "audio/wav",
                    streamType = "BUFFERED", // BUFFERED or LIVE
                    metadata = new Metadata
                    {
                        type = 0,
                        metadataType = 0,
                        title = "Desktop Stream",
                        images = new List<Image>()
                    },
                },
                requestId = 1
            };
            return GetCastMessage(message, namespaceMedia, sourceId, destinationId);
        }

        public CastMessage GetPauseMessage(string sessionId, int mediaSessionId, int requestId, string sourceId, string destinationId)
        {
            return GetCastMessage(new MessagePause { type = "PAUSE", sessionId = sessionId, mediaSessionId = mediaSessionId, requestId = requestId }, namespaceMedia, sourceId, destinationId);
        }

        public CastMessage GetPingMessage()
        {
            return GetCastMessage(new PayloadMessageBase { type = "PING" }, namespaceHeartbeat);
        }

        public CastMessage GetPongMessage()
        {
            return GetCastMessage(new PayloadMessageBase { type = "PONG" }, namespaceHeartbeat);
        }

        public CastMessage GetReceiverStatusMessage(int requestId)
        {
            return GetCastMessage(new MessageStatus { type = "GET_STATUS", requestId = requestId }, namespaceReceiver);
        }

        public CastMessage GetMediaStatusMessage(int requestId, string sourceId, string destinationId)
        {
            return GetCastMessage(new MessageStatus { type = "GET_STATUS", requestId = requestId }, namespaceMedia, sourceId, destinationId);
        }

        public CastMessage GetStopMessage(string sessionId, int mediaSessionId, int requestId, string sourceId, string destinationId)
        {
            return GetCastMessage(new MessagePause { type = "STOP", sessionId = sessionId, mediaSessionId = mediaSessionId, requestId = requestId }, namespaceMedia, sourceId, destinationId);
        }

        public CastMessage GetCastMessage(PayloadMessageBase message, string msgNamespace, string sourceId = null, string destinationId = null)
        {
            if (string.IsNullOrWhiteSpace(sourceId)) sourceId = "sender-0";
            if (string.IsNullOrWhiteSpace(destinationId)) destinationId = "receiver-0";

            var jsonMessage = JsonConvert.SerializeObject(message);
            return new CastMessage.Builder
            {
                ProtocolVersion = 0,
                SourceId = sourceId,
                DestinationId = destinationId,
                PayloadType = 0,
                Namespace = msgNamespace,
                PayloadUtf8 = jsonMessage
            }.Build();
        }

        public byte[] MessageToByteArray(CastMessage message)
        {
            var messageStream = new MemoryStream();
            message.WriteTo(messageStream);
            var bufMsg = messageStream.ToArray();

            var bufLen = new byte[4];
            bufLen = BitConverter.GetBytes(bufMsg.Length);
            bufLen = bufLen.Reverse().ToArray();

            return bufLen.Concat(bufMsg).ToArray();
        }
    }
}
