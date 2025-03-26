using System.Collections.Concurrent;
using NetworkLibrary.Utils;

namespace NetworkLibrary.Networks.Packet
{
    public class PacketFactory
    {
        private readonly ConcurrentDictionary<int, Type> _packets = new();
        public IReadOnlyDictionary<int, Type> Packets => _packets;

        public PacketFactory RegisterPacket(params IPacket[] packets)
        {
            foreach (var packet in packets)
            {
                if (!_packets.TryAdd(packet.PacketPrimaryKey, packet.GetType()))
                {
                    _packets.TryGetValue(packet.PacketPrimaryKey, out var already);
                    throw new ArgumentException($"Packet Key Already Exists. Already: {already}, New: {packet.GetType()}");
                }
            }
            return this;
        }

        internal IPacket? Handle(ByteBuf buf)
        {
            if (!_packets.TryGetValue(buf.ReadVarInt(), out var type)) return null;
            if (Activator.CreateInstance(type) is not IPacket packet) return null;
            packet.Read(buf);
            return packet;
        }
    }
}
