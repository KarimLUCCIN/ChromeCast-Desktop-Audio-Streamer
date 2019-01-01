using ChromeCast.Desktop.AudioStreamer.ProtocolBuffer;
using ChromeCast.Library.Communication;
using ChromeCast.Library.Streaming;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace ChromeCast.Library.Networking
{
    public class EndpointConnection : IDisposable
    {
        public event Action<EndpointConnection, DeviceConnectionState> StateChanged;
        public event Action<EndpointConnection, ArraySegment<byte>> DataReceived;
        public event Action<EndpointConnection, CastMessage> MessageReceived;

        public string Host { get; private set; }

        private DeviceConnectionState connectionState = DeviceConnectionState.None;
        public DeviceConnectionState ConnectionState
        {
            get { return connectionState; }
            set
            {
                if (connectionState != value)
                {
                    connectionState = value;
                    StateChanged?.Invoke(this, value);
                }
            }
        }

        private TcpClient tcpClient;
        private SslStream sslStream;
        private const int bufferSize = 2048;
        private byte[] receiveBuffer;
        private DeviceReceiveBuffer deviceReceiveBuffer;
        private StreamingConnection streamingConnection;

        public EndpointConnection(string host)
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                throw new ArgumentException("The host isn't valid", nameof(host));
            }

            Host = host;
            deviceReceiveBuffer = new DeviceReceiveBuffer();
            deviceReceiveBuffer.MessageReceived += (CastMessage msg) =>
            {
                MessageReceived?.Invoke(this, msg);
            };
        }

        public async Task<bool> ConnectAsync()
        {
            switch (ConnectionState)
            {
                case DeviceConnectionState.Connecting:
                    throw new InvalidOperationException("Already trying to connect");
                case DeviceConnectionState.Connected:
                    return true;
            }

            try
            {
                ConnectionState = DeviceConnectionState.Connecting;
                try
                {
                    tcpClient = new TcpClient();
                    tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                    await tcpClient.ConnectAsync(Host, 8009);

                    sslStream = new SslStream(tcpClient.GetStream(), false, delegate { return true; }, null);
                    await sslStream.AuthenticateAsClientAsync(Host, new X509CertificateCollection(), SslProtocols.Tls12, false);
                    if (StartReceive())
                    {
                        ConnectionState = DeviceConnectionState.Connected;
                        return true;
                    }
                    else
                    {
                        ConnectionState = DeviceConnectionState.Error;
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                    ConnectionState = DeviceConnectionState.Error;
                    return false;
                }
            }
            finally
            {
                if (ConnectionState != DeviceConnectionState.Connected)
                {
                    // Something went wrong
                    Dispose(resetState: false);
                }
            }
        }

        public async Task ConnectRecordingDataAsync(Socket socket)
        {
            if (socket == null)
            {
                throw new ArgumentNullException(nameof(socket));
            }

            streamingConnection = new StreamingConnection();
            streamingConnection.SetSocket(socket);
            await Task.Run(() =>
            {
                streamingConnection.SendStartStreamingResponse();
            });
        }

        public async Task SendRecordingDataAsync(ArraySegment<byte> dataToSend, WaveFormat format)
        {
            if (streamingConnection == null)
            {
                return;
            }

            if (streamingConnection.IsConnected)
            {
                await Task.Run(() => streamingConnection?.SendData(dataToSend, format));
            }
            else
            {
                streamingConnection = null;
            }
        }

        public async Task SendMessageAsync(ArraySegment<byte> send)
        {
            if (!await ConnectAsync())
            {
                ConnectionState = DeviceConnectionState.Error;
                return;
            }

            if (tcpClient != null && tcpClient.Client != null && tcpClient.Connected)
            {
                try
                {
                    await sslStream.WriteAsync(send.Array, send.Offset, send.Count);
                    await sslStream.FlushAsync();
                }
                catch (Exception)
                {
                }
            }
        }

        private bool StartReceive()
        {
            try
            {
                receiveBuffer = new byte[bufferSize];
                sslStream.BeginRead(receiveBuffer, 0, receiveBuffer.Length, DataReceivedInternal, sslStream);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return false;
            }
        }

        private void DataReceivedInternal(IAsyncResult ar)
        {
            SslStream stream = (SslStream)ar.AsyncState;
            int byteCount = -1;

            try
            {
                byteCount = stream.EndRead(ar);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                ConnectionState = DeviceConnectionState.Error;
                Dispose(resetState: false);
            }

            if (byteCount > 0)
            {
                var data = new ArraySegment<byte>(receiveBuffer, 0, byteCount);
                DataReceived?.Invoke(this, data);
                deviceReceiveBuffer.OnReceive(data);
            }

            StartReceive();
        }

        public void Dispose(bool resetState)
        {
            sslStream?.Close();
            tcpClient?.Close();

            if (resetState)
            {
                ConnectionState = DeviceConnectionState.None;
            }
        }

        public void Dispose()
        {
            Dispose(resetState: true);
        }
    }
}
