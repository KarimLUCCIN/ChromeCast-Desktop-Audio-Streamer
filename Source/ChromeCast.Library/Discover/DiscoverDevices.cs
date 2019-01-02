using System;
using System.Timers;
using Rssdp;
using System.Linq;
using Zeroconf;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading;

namespace ChromeCast.Library.Discover
{
    public class DiscoverDevices
    {
        public const int Interval = 2000;
        public const int MaxNumberOfTries = 15;
        private DiscoverServiceSSDP discoverServiceSSDP;

        public DiscoverDevices(DiscoverServiceSSDP discoverServiceSSDPIn)
        {
            discoverServiceSSDP = discoverServiceSSDPIn;
        }

        public void BeginDiscover(Action<(DiscoveredSsdpDevice device, SsdpDevice fullDevice)> callback)
        {
            SynchronizationContext syncContext = SynchronizationContext.Current;

            // SSDP search
            discoverServiceSSDP.BeginDiscover(((DiscoveredSsdpDevice device, SsdpDevice fullDevice) newItem) => {
                syncContext.Post(_ =>
                {
                    callback(newItem);
                }, null);
            });

            // MDNS search
            BeginMdnsSearch(((DiscoveredSsdpDevice device, SsdpDevice fullDevice) newItem) => {
                syncContext.Post(_ =>
                {
                    callback(newItem);
                }, null);
            });
        }

        public void BeginMdnsSearch(Action<(DiscoveredSsdpDevice device, SsdpDevice fullDevice)> callback)
        {
            ZeroconfResolver.BrowseDomainsAsync(scanTime: new TimeSpan(1000000000), retries: 5, callback: async (string protocol, string rawIpAddress) => {
                if (protocol.StartsWith("_googlecast"))
                {
                    var ipAddress = rawIpAddress.Contains(':') ? rawIpAddress.Substring(0, rawIpAddress.IndexOf(':')) : rawIpAddress;

                    var item = (
                        new DiscoveredSsdpDevice { DescriptionLocation = new Uri($"http://{ipAddress}"), Usn = ipAddress },
                        new SsdpRootDevice { FriendlyName = await GetDeviceFriendlyNameAsync(ipAddress) }
                    );

                    callback(item);
                }
            }).Forget();
        }

        private async Task<string> GetDeviceFriendlyNameAsync(string ipAddress)
        {
            var friendlyName = "";
            try
            {
                var http = new HttpClient();
                var responce = await http.GetAsync($"http://{ipAddress}:8008/setup/eureka_info?options=detail");
                var json = await responce.Content.ReadAsStringAsync();
                var info = JsonConvert.DeserializeObject<EurekaInfo>(json);
                friendlyName = info.Name;
            }
            catch (Exception)
            {
                friendlyName = ipAddress;
            }
            return friendlyName;
        }
    }

    public class EurekaInfo
    {
        public string Name { get; set; }
    }
}