using Q42.HueApi;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MiniCast.Hue
{
    public static class HueEndpointsEnumerator
    {
        public static async Task<IEnumerable<HueEndpoint>> EnumerateDevices(TimeSpan? scanningTime = null)
        {
            var locator = new HttpBridgeLocator();
            var ips = await locator.LocateBridgesAsync(scanningTime ?? TimeSpan.FromSeconds(5));

            return
                from endpoint in ips
                select new HueEndpoint(endpoint);
        }
    }
}
