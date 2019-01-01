using ChromeCast.Desktop.AudioStreamer.ProtocolBuffer;
using System;
using System.IO;
using System.Linq;

namespace ChromeCast.Library.Communication
{
    public class DeviceReceiveBuffer
    {
        public Action<CastMessage> MessageReceived;

        public void OnReceive(ArraySegment<byte> data)
        {
            ParseMessages(data);
        }

        private void ParseMessages(ArraySegment<byte> serverMessage)
        {
            int offset = 0;

            while (serverMessage.Count - offset >= 4)
            {
                var messageSize = BitConverter.ToInt32(serverMessage.Skip(offset).Take(4).Reverse().ToArray(), 0);
                if (messageSize == 0)
                    break;

                if (serverMessage.Count >= 4 + messageSize)
                {
                    var message = new ArraySegment<byte>(serverMessage.Array, serverMessage.Offset + 4, messageSize);
                    ProcessMessage(message);

                    offset = offset + 4 + messageSize;
                }
            }
        }

        private void ProcessMessage(ArraySegment<byte> message)
        {
            using (var ms = new MemoryStream(message.Array, message.Offset, message.Count))
            {
                var castMessage = CastMessage.ParseFrom(ms);
                MessageReceived?.Invoke(castMessage);
            }
        }
    }
}
