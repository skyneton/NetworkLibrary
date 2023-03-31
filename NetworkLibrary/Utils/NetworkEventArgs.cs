using NetworkLibrary.Networks;

namespace NetworkLibrary.Utils
{
    public class NetworkEventArgs
    {
        public readonly Network? network;
        internal NetworkEventArgs(Network? network)
        {
            this.network = network;
        }
    }
}
