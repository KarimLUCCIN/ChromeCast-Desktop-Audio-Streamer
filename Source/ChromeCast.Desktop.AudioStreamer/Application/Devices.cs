﻿using System;
using System.Net;
using System.Linq;
using System.Collections.Generic;
using System.Net.Sockets;
using Rssdp;
using NAudio.Wave;
using Microsoft.Practices.Unity;
using ChromeCast.Desktop.AudioStreamer.Communication;
using ChromeCast.Desktop.AudioStreamer.Classes;
using System.Timers;

namespace ChromeCast.Desktop.AudioStreamer.Application
{
    public class Devices
    {
        private List<Device> deviceList = new List<Device>();
        private Action<Device> onAddDeviceCallback;
        private bool AutoStart;
        private MainForm mainForm;
        private ApplicationLogic applicationLogic;
        private Logger logger;
        private string streamingUrl;

        public Devices(Logger logger)
        {
            this.logger = logger;
        }

        public void OnDeviceAvailable(DiscoveredSsdpDevice discoveredSsdpDevice, SsdpDevice ssdpDevice)
        {
            AddDevice(discoveredSsdpDevice, ssdpDevice);
        }

        private void AddDevice(DiscoveredSsdpDevice device, SsdpDevice fullDevice)
        {
            var existingDevice = deviceList.FirstOrDefault(d => d.GetHost().Equals(device.DescriptionLocation.Host));
            if (existingDevice == null)
            {
                if (!deviceList.Any(d => d.GetUsn() != null && d.GetUsn().Equals(device.Usn)))
                {
                    var newDevice = new Device(new DeviceConnection(logger, new DeviceReceiveBuffer()), new DeviceCommunication(logger, new ChromeCastMessages()));
                    newDevice.SetStreamingUrl(streamingUrl);
                    newDevice.SetDiscoveredDevices(device, fullDevice);
                    deviceList.Add(newDevice);
                    onAddDeviceCallback?.Invoke(newDevice);

                    if (AutoStart)
                        newDevice.OnClickDeviceButton(null, null);
                }
            }
            else
            {
                existingDevice.SetDiscoveredDevices(device, fullDevice);
                existingDevice.GetDeviceControl()?.SetDeviceName(existingDevice.GetFriendlyName());
                existingDevice.GetMenuItem().Text = existingDevice.GetFriendlyName();
            }
        }

        public void VolumeUp()
        {
            foreach (var device in deviceList)
            {
                device.VolumeUp();
            }
        }

        public void VolumeDown()
        {
            foreach (var device in deviceList)
            {
                device.VolumeDown();
            }
        }

        public void VolumeMute()
        {
            foreach (var device in deviceList)
            {
                device.VolumeMute();
            }
        }

        public bool Stop()
        {
            var playing = false;
            foreach (var device in deviceList)
            {
                if (device.GetDeviceState().Equals(DeviceState.Playing))
                {
                    playing = true;
                    device.Stop();
                }
            }
            return playing;
        }

        public void SetStreamingUrl(string streamingUrl)
        {
            this.streamingUrl = streamingUrl;

            foreach (var device in deviceList)
            {
                device.SetStreamingUrl(streamingUrl);
            }
        }

        public void Start()
        {
            foreach (var device in deviceList)
            {
                device.Start();
            }
        }

        public void AddStreamingConnection(Socket socket, string httpRequest)
        {
            var remoteAddress = ((IPEndPoint)socket.RemoteEndPoint).Address.ToString();
            foreach (var device in deviceList)
            {
                if (device.AddStreamingConnection(remoteAddress, socket))
                    break;
            }
        }

        public void OnRecordingDataAvailable(ArraySegment<byte> dataToSend, WaveFormat format)
        {
            foreach (var device in deviceList)
            {
                device.OnRecordingDataAvailable(dataToSend, format);
            }
        }

        public void OnGetStatus()
        {
            foreach (var device in deviceList)
            {
                if (device.IsConnected())
                    device.OnGetStatus();
            }
        }

        public void SetAutoStart(bool autoStartIn)
        {
            AutoStart = autoStartIn;
        }

        public void Dispose()
        {
            foreach (var device in deviceList)
            {
                device.SetDeviceState(DeviceState.Disposed);
            }
        }

        public void SetCallback(Action<Device> onAddDeviceCallbackIn)
        {
            onAddDeviceCallback = onAddDeviceCallbackIn;
        }

        public int Count()
        {
            return deviceList.Count();
        }

        public void Sync()
        {
            if (mainForm.DoSyncDevices())
            {
                mainForm.SetLagValue(2);
                applicationLogic.SetLagThreshold(2);

                var timerReset = new Timer { Interval = 3000, Enabled = true };
                timerReset.Elapsed += new ElapsedEventHandler(ResetLagThreshold);
                timerReset.Start();
            }
        }

        private void ResetLagThreshold(object sender, ElapsedEventArgs e)
        {
            mainForm.SetLagValue(1000);
            applicationLogic.SetLagThreshold(1000);
            ((Timer)sender).Stop();
        }

        public void SetDependencies(MainForm mainFormIn, ApplicationLogic applicationLogicIn)
        {
            mainForm = mainFormIn;
            applicationLogic = applicationLogicIn;
        }
    }
}
