using NetworkLibrary.Networks;
using NetworkLibrary.Networks.Packet;

namespace SampleServer
{
    internal class PacketHandler : IPacketHandler
    {
        public void Handle(Network network, IPacket packet)
        {
            Console.WriteLine(((SamplePacket)packet).Data);
        }
    }
}
