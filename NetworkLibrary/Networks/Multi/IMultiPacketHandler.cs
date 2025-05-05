using NetworkLibrary.Networks.Packet;

namespace NetworkLibrary.Networks.Multi
{
    public interface IMultiPacketHandler
    {
        void Handle(MultiNetwork network, IPacket packet);
    }
}
