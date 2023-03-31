using NetworkLibrary.Network.Packet;
using NetworkLibrary.Utils;
using System.Collections.Concurrent;

namespace PatientSignServerService.Networks.Packet
{
    public class PacketFactory
    {
        private readonly ConcurrentDictionary<int, Type> Packets = new();

        public void RegisterPacket(params IPacket[] packets)
        {
            foreach(var packet in packets)
            {
                if (!Packets.TryAdd(packet.PacketPrimaryKey, packet.GetType()))
                {
                    Packets.TryGetValue(packet.PacketPrimaryKey, out var already);
                    throw new ArgumentException($"Packet Key Already Exists. Already: {already}, New: {packet.GetType()}");
                }
            }
        }

        internal IPacket? Handle(ByteBuf buf)
        {
            if (!Packets.TryGetValue(buf.ReadVarInt(), out var type)) return null;
            if (Activator.CreateInstance(type) is not IPacket packet) return null;
            packet.Read(buf);
            return packet;
        }
    }
}
