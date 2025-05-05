using System.Net.Sockets;
using NetworkLibrary.Networks;

namespace NetworkLibrary.Utils
{
    internal class MultiStateObject
    {
        public readonly byte[] Buffer;
        public readonly NetworkBuf NetworkBuffer;
        public Socket TargetSocket { get; }

        public MultiStateObject(Socket socket, int bufferSize)
        {
            TargetSocket = socket;
            NetworkBuffer = new NetworkBuf(bufferSize);
            Buffer = new byte[bufferSize];
        }
    }
}
