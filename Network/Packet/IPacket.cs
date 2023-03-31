using NetworkLibrary.Utils;

namespace NetworkLibrary.Network.Packet
{
    public interface IPacket
    {
        int PacketPrimaryKey { get; }
        void Write(ByteBuf buf);
        void Read(ByteBuf buf);
    }
}
