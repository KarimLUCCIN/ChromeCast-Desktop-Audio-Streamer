using System;
using System.Text;
using System.Net.Sockets;
using NAudio.Wave;

namespace ChromeCast.Library.Streaming
{
    public class StreamingConnection
    {
        private Socket Socket;
        private bool isRiffHeaderSent;

        public StreamingConnection()
        {
            isRiffHeaderSent = false;
        }

        public void SendData(ArraySegment<byte> dataToSend, WaveFormat format)
        {
            if (!isRiffHeaderSent)
            {
                isRiffHeaderSent = true;
                Send(new ArraySegment<byte>(Riff.GetRiffHeader(format)));
            }

            Send(dataToSend);
        }

        public void Send(ArraySegment<byte> data)
        {
            if (Socket != null && Socket.Connected)
            {
                try
                {
                    Socket.Send(new[] { data });
                }
                catch (Exception)
                {
                }
            }
        }

        public void SendStartStreamingResponse()
        {
            var startStreamingResponse = Encoding.ASCII.GetBytes(GetStartStreamingResponse());
            Send(new ArraySegment<byte>(startStreamingResponse));
        }

        private string GetStartStreamingResponse()
        {
            var httpStartStreamingReply = new StringBuilder();

            httpStartStreamingReply.Append("HTTP/1.0 200 OK\r\n");
            httpStartStreamingReply.Append("Content-Disposition: inline; filename=\"stream.wav\"\r\n");
            httpStartStreamingReply.Append("Content-Type: audio/wav\r\n");
            httpStartStreamingReply.Append("Connection: keep-alive\r\n");
            httpStartStreamingReply.Append("\r\n");

            return httpStartStreamingReply.ToString();
        }

        public bool IsConnected
        {
            get
            {
                return Socket != null && Socket.Connected;
            }
        }

        public string GetRemoteEndPoint()
        {
            if (Socket == null)
                return string.Empty;

            return Socket.RemoteEndPoint.ToString();
        }

        public void SetSocket(Socket socketIn)
        {
            Socket = socketIn;
        }
    }
}
