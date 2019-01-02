using Q42.HueApi;
using Q42.HueApi.Models.Bridge;

namespace MiniCast.Hue
{
    public class HueEndpoint
    {
        public string Id => Endpoint.BridgeId;
        public string Address => Endpoint.IpAddress;

        public LocatedBridge Endpoint { get; private set; }
        public LocalHueClient Client { get; private set; }

        public HueEndpoint(LocatedBridge endpoint)
        {
            Endpoint = endpoint ?? throw new System.ArgumentNullException(nameof(endpoint));
            Client = new LocalHueClient(Endpoint.IpAddress);
        }
    }
}