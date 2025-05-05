using NetworkLibrary.Networks;
using NetworkLibrary.Networks.Multi;

namespace NetworkLibrary.Utils
{
    public class NetworkEventArgs
    {
        public readonly Network? Network;
        internal NetworkEventArgs(Network? network)
        {
            Network = network;
        }
    }
    public class MultiNetworkEventArgs
    {
        public readonly MultiNetwork? Network;
        internal MultiNetworkEventArgs(MultiNetwork? network)
        {
            Network = network;
        }
    }
}
