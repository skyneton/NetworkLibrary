using NetworkLibrary.Utils;
using System;

namespace NetworkLibrary.Networks
{
    public class NetworkBuf
    {
        private byte[] _buf = new byte[1024 * 2];
        private int _offset;

        public void Clear()
        {
            _offset = 0;
        }

        public byte[] ReadPacket()
        {
            if (_offset <= 0) return Array.Empty<byte>();
            var offset = ByteBuf.ReadVarInt(_buf, out var length);

            if (_offset < offset + length) return Array.Empty<byte>();

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
            if(_buf.Length >= _offset + size) return;
            var buf = new byte[_buf.Length + size];
            Array.Copy(_buf, buf, _offset);
            _buf = buf;
        }
    }
}
