using System.Net.Sockets;

namespace NetworkLibrary.Utils
{
    internal class StateObject
    {
        public readonly byte[] Buffer;
        public Socket TargetSocket { get; }

        public StateObject(Socket socket, int bufferSize)
        {
            TargetSocket = socket;
            Buffer = new byte[bufferSize];
        }
    }
}
