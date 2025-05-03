using NetworkLibrary.Networks;

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
}
