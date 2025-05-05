using System.Net.Sockets;
using NetworkLibrary.Utils;

namespace NetworkLibrary.Networks.Multi
{
    internal class TemporaryMultiNetwork
    {
        private readonly Socket _socket;
        public Socket Socket => _socket;
        private readonly IMultiNetworkSocket _parent;
        private readonly NetworkBuf _receiveBuf = new(1024 * 2);
        public NetworkBuf Buffer => _receiveBuf;
        private readonly CancellationTokenSource token = new();
        public TemporaryMultiNetwork(Socket socket, IMultiNetworkSocket parent)
        {
            socket.NoDelay = true;
            _socket = socket;
            _parent = parent;
        }

        public void Close()
        {
            token.Cancel();
        }

        public void Disconnect()
        {
            token.Cancel();
            _socket.Close();
        }

        internal void SocketBounding()
        {
            Task.Run(() =>
            {
                var buf = new byte[512];
                while (_socket.Connected)
                {
                    var read = _socket.Receive(buf);
                    _receiveBuf.Read(buf, read);
                    var result = _receiveBuf.ReadPacket();
                    while (result.Length > 0)
                    {
                        Receive(result);
                        return;
                    }
                }
            }, token.Token);
        }

        private void Receive(byte[] buffer)
        {
            var buf = new ByteBuf(buffer);
            if (buf.ReadBool())
            {
                _parent.BindConnecting(this);
                return;
            }
            var size = buf.ReadVarInt();
            var id = new Guid(buf.Read(buf.Length));
            if (size > 0)
                _parent.BindConnecting(this, size, id);
            else
                _parent.BindConnecting(this, id);
        }

        public void Send(int size, Guid guid)
        {
            var buf = new ByteBuf();
            buf.WriteByte(0);
            buf.WriteVarInt(size);
            buf.Write(guid.ToByteArray());
            _socket.Send(buf.Flush());
        }

        public void Send(Guid guid)
        {
            var buf = new ByteBuf();
            buf.WriteByte(0);
            buf.WriteByte(0);
            buf.Write(guid.ToByteArray());
            _socket.Send(buf.Flush());
        }

        public void Send()
        {
            var buf = new ByteBuf();
            buf.WriteByte(1);
            _socket.Send(buf.Flush());
        }
    }
}
