﻿using System;
using Rssdp;
using System.Net;
using System.Net.Sockets;

namespace ChromeCast.Library.Discover
{
    public class DiscoverServiceSSDP
    {
        private const string ChromeCastUpnpDeviceType = "urn:dial-multiscreen-org:device:dial:1";
        private Action<DiscoveredSsdpDevice, SsdpDevice> onDiscovered;
        private Action updateCounter;

        public void Discover(Action<DiscoveredSsdpDevice, SsdpDevice> onDiscoveredIn, Action updateCounterIn)
        {
            onDiscovered = onDiscoveredIn;
            updateCounter = updateCounterIn;

            var ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            var ipAddresses = GetIpAddresses(ipHostInfo);
            foreach (var ipAddress in ipAddresses)
            {
                using (var deviceLocator =
                    new SsdpDeviceLocator(
                        communicationsServer: new Rssdp.Infrastructure.SsdpCommunicationsServer(
                            new SocketFactory(ipAddress: ipAddress.ToString())
                        )
                    ))
                {
                    deviceLocator.NotificationFilter = ChromeCastUpnpDeviceType;
                    deviceLocator.DeviceAvailable += OnDeviceAvailable;
                    deviceLocator.SearchAsync();
                }
            }
        }

        private async void OnDeviceAvailable(object sender, DeviceAvailableEventArgs e)
        {
            var fullDevice = await e.DiscoveredDevice.GetDeviceInfo();
            onDiscovered?.Invoke(e.DiscoveredDevice, fullDevice);
            updateCounter?.Invoke();
        }

        private IPAddress[] GetIpAddresses(IPHostEntry ipHostInfo)
        {
            return ipHostInfo.AddressList;
        }
    }
}
