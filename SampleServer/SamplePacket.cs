using NetworkLibrary.Networks.Packet;
using NetworkLibrary.Utils;

namespace SampleServer
{
    public class SamplePacket : IPacket
    {
        public int PacketPrimaryKey => 0;
        public string Data { get; private set; } = string.Empty;
        /// <summary>
        /// You must create non parameter Constructor.<br/>
        /// Because when packet received create new Packet instance.
        /// </summary>
        public SamplePacket() { }
        public SamplePacket(string data)
        {
            Data = data;
        }

        public void Read(ByteBuf buf)
        {
            Data = buf.ReadString();
        }

        public void Write(ByteBuf buf)
        {
            buf.WriteString(Data);
        }
    }
}
