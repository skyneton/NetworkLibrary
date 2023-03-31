namespace NetworkLibrary.Network.Packet
{
    public interface IPacketHandler
    {
        void Handle(NetworkManager network, IPacket packet);
    }
}
