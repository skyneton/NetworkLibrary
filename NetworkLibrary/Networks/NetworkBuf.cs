using NetworkLibrary.Utils;

namespace NetworkLibrary.Networks
{
    public class NetworkBuf(int size)
    {
        private byte[] _buf = new byte[size];
        public byte[] Buffer => _buf;
        private int _offset;
        public int WritedLength => _offset;

        public void Clear()
        {
            _offset = 0;
        }

        public byte[] ReadPacket()
        {
            if (_offset <= 0) return [];
            var offset = ByteBuf.ReadVarInt(_buf, out var length);

            if (_offset < offset + length) return [];

            var result = new byte[length];
            Array.Copy(_buf, offset, result, 0, length);

            var block = offset + length;
            _offset -= block;
            Array.Copy(_buf, block, _buf, 0, _offset);

            return result;
        }

        public virtual void Read(byte[] input, int size)
        {
            SizeGrow(size);
            Array.Copy(input, 0, _buf, _offset, size);
            _offset += size;
        }

        private void SizeGrow(int size)
        {
            if (_buf.Length >= _offset + size) return;
            var buf = new byte[_buf.Length + size];
            Array.Copy(_buf, buf, _offset);
            _buf = buf;
        }
    }
}
