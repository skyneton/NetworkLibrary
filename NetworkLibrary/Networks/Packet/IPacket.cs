using NetworkLibrary.Utils;

namespace NetworkLibrary.Networks.Packet
{
    public interface IPacket
    {
        /// <summary>
        /// Packet distinguishing ID.
        /// </summary>
        int PacketPrimaryKey { get; }

        /// <summary>
        /// Work Packet send.
        /// </summary>
        /// <param name="buf">
        /// Like MemoryStream.
        /// <example>
        /// <code>
        /// buf.WriteVarInt(10);
        /// buf.WriteString("ASDF");
        /// </code>
        /// </example>
        /// </param>
        void Write(ByteBuf buf);
        /// <summary>
        /// Work Packet read.
        /// </summary>
        /// <param name="buf">
        /// Like MemoryStream.
        /// <example>
        /// <code>
        /// buf.ReadVarInt();
        /// buf.ReadString();
        /// </code>
        /// </example>
        /// </param>
        void Read(ByteBuf buf);
    }
}
