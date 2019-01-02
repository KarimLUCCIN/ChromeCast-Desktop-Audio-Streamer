using System;
using Rssdp;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ChromeCast.Library.Discover
{
    public class DiscoverServiceSSDP
    {
        private const string ChromeCastUpnpDeviceType = "urn:dial-multiscreen-org:device:dial:1";

        public async void BeginDiscover(Action<(DiscoveredSsdpDevice device, SsdpDevice fullDevice)> callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

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
                    var devices = await deviceLocator.SearchAsync();

                    foreach(var device in devices)
                    {
                        var fullDevice = await device.GetDeviceInfo();
                        callback((device, fullDevice));
                    }
                }
            }
        }

        private IPAddress[] GetIpAddresses(IPHostEntry ipHostInfo)
        {
            return ipHostInfo.AddressList;
        }
    }
}
