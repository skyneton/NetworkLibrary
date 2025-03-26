using NetworkLibrary.Networks.Packet;
using NetworkLibrary.Networks;

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
