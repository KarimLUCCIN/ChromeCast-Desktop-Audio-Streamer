using Q42.HueApi;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using Q42.HueApi.NET;
using Q42.HueApi.Models.Bridge;

namespace MiniCast.Hue
{
    public static class HueEndpointsEnumerator
    {
        public static async Task<IEnumerable<HueEndpoint>> EnumerateDevices(TimeSpan? scanningTime = null)
        {
            var httpLocatorTask = (new HttpBridgeLocator()).LocateBridgesAsync(scanningTime ?? TimeSpan.FromSeconds(5));
            var ssdpLocatorTask = (new SSDPBridgeLocator()).LocateBridgesAsync(scanningTime ?? TimeSpan.FromSeconds(5));

            var foundBridges = new List<LocatedBridge>();

            try
            {
                foreach (var bridge in await httpLocatorTask)
                {
                    foundBridges.Add(bridge);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            try
            {
                foreach (var bridge in await ssdpLocatorTask)
                {
                    if (!foundBridges.Exists((b) => b.BridgeId == bridge.BridgeId || b.IpAddress == bridge.IpAddress))
                    {
                        foundBridges.Add(bridge);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            return
                from endpoint in foundBridges
                select new HueEndpoint(endpoint);
        }
    }
}
