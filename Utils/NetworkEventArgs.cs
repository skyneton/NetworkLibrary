using NetworkLibrary.Network;

namespace NetworkLibrary.Utils
{
    public class NetworkEventArgs
    {
        public readonly NetworkManager? network;
        internal NetworkEventArgs(NetworkManager? network)
        {
            this.network = network;
        }
    }
}
