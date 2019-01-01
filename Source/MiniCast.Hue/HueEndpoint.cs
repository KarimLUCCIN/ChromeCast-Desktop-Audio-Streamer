using Q42.HueApi.Models.Bridge;

namespace MiniCast.Hue
{
    public class HueEndpoint
    {
        private LocatedBridge endpoint;

        public string Id => endpoint.BridgeId;
        public string Address => endpoint.IpAddress;

        public HueEndpoint(LocatedBridge endpoint)
        {
            this.endpoint = endpoint ?? throw new System.ArgumentNullException(nameof(endpoint));
        }
    }
}