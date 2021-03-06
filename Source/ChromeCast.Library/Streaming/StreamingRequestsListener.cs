﻿using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ChromeCast.Library.Classes;

namespace ChromeCast.Library.Streaming
{
    public class StateObject
    {
        public Socket workSocket = null;
        public const int bufferSize = 2048;
        public byte[] buffer;
        public StringBuilder receiveBuffer = new StringBuilder();
    }

    public class StreamingRequestsListener
    {
        public ManualResetEvent allDone = new ManualResetEvent(false);
        public event Action<string, int> Listening;
        public event Action<Socket, string> Connected;
        private Socket listener;

        public IPEndPoint StartListening(IPAddress ipAddress)
        {
            var localEndPoint = new IPEndPoint(ipAddress, 0);
            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);
                Listening?.Invoke(((IPEndPoint)listener.LocalEndPoint).Address.ToString(), ((IPEndPoint)listener.LocalEndPoint).Port);

                var result = ((IPEndPoint)listener.LocalEndPoint);
                Task.Run(() =>
                {
                    try
                    {
                        while (true)
                        {
                            allDone.Reset();
                            listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
                            allDone.WaitOne();
                        }
                    }
                    catch(Exception ex)
                    {
                        Debug.WriteLine(ex.ToString());
                    }
                });

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return null;
            }
        }

        public void StopListening()
        {
            try
            {
                if (listener != null)
                    listener.Close();
            }
            catch (Exception)
            {
            }
        }

        private void AcceptCallback(IAsyncResult asyncResult)
        {
            allDone.Set();

            var listener = (Socket)asyncResult.AsyncState;
            try
            {
                var handlerSocket = listener.EndAccept(asyncResult);
                var state = new StateObject { workSocket = handlerSocket };

                state.buffer = new byte[StateObject.bufferSize];
                handlerSocket.BeginReceive(state.buffer, 0, StateObject.bufferSize, 0, new AsyncCallback(ReadCallback), state);
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private void ReadCallback(IAsyncResult asyncResult)
        {
            var state = (StateObject)asyncResult.AsyncState;
            var handlerSocket = state.workSocket;

            var bytesRead = handlerSocket.EndReceive(asyncResult);
            if (bytesRead > 0)
            {
                state.receiveBuffer.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
                if (state.receiveBuffer.ToString().IndexOf("\r\n\r\n") >= 0)
                {
                    Connected?.Invoke(handlerSocket, state.receiveBuffer.ToString());
                }
                else
                {
                    // Not all data received. Get more.  
                    handlerSocket.BeginReceive(state.buffer, 0, StateObject.bufferSize, 0, new AsyncCallback(ReadCallback), state);
                }
            }
        }
    }
}