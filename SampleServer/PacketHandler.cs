using NetworkLibrary.Networks.Packet;
using NetworkLibrary.Networks;
using NetworkLibrary.Networks.Multi;

namespace SampleServer
{
    internal class MultiPacketHandler : IMultiPacketHandler
    {
        public void Handle(MultiNetwork network, IPacket packet)
        {
            Console.WriteLine("client: " + ((SamplePacket)packet).Data);
        }
    }
    internal class PacketHandler : IPacketHandler
    {
        public void Handle(Network network, IPacket packet)
        {
            Console.WriteLine(((SamplePacket)packet).Data);
        }
    }
}
