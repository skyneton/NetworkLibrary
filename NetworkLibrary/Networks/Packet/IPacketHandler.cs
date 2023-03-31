namespace NetworkLibrary.Networks.Packet
{
    public interface IPacketHandler
    {
        void Handle(Network network, IPacket packet);
    }
}
