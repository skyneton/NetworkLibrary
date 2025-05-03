using System.Collections.Concurrent;
using System.Net.Sockets;
using NetworkLibrary.Networks.Compress;
using NetworkLibrary.Networks.Packet;
using NetworkLibrary.Utils;

namespace NetworkLibrary.Networks
{
    public class Network
    {
        /// <summary>
        /// Socket retention time. Do not check if 0.<br/>
        /// If using this, safe set > 800 or set > 50.<br/>
        /// Because frame update every 50 millis and first update 800 millis.
        /// </summary>
        public long KeepAliveTimeout = 0;
        public readonly PacketCompression Compression;
        public readonly Socket Socket;
        public bool Connected => Socket?.Connected ?? false;
        public bool IsAvailable { get; private set; }
        public long LastPacketMillis { get; private set; } = TimeManager.CurrentTimeInMillis;

        /// <summary>
        /// When created instance, initalized DefaultPacketFactory(On Listener) or linked client packetFactory.
        /// </summary>
        public PacketFactory PacketFactory { get; set; }

        private readonly NetworkBuf _receiveBuf = new();

        /// <summary>
        /// Final/First Byte IO
        /// </summary>
        public IRawHandler? RawHandler { get; set; }
        public ICompressor Compressor = new ZlibCompressor();
        private IPacketHandler? _packetHandler;
        public IPacketHandler? PacketHandler
        {
            get => _packetHandler;
            set
            {
                _packetHandler = value;
                while (!_receivePacket.IsEmpty)
                {
                    if (!_receivePacket.TryDequeue(out var packet)) continue;
                    _packetHandler?.Handle(this, packet);
                }
            }
        }
        private readonly ConcurrentQueue<IPacket> _receivePacket = new();

        public Network(Socket socket)
        {
            socket.NoDelay = true;

            Socket = socket;

            IsAvailable = true;
            Compression = new PacketCompression();
        }

        internal void BeginReceive(int bufferSize)
        {
            var so = new StateObject(Socket, bufferSize);
            so.TargetSocket.BeginReceive(so.Buffer, 0, so.Buffer.Length, SocketFlags.None, ReceiveAsync, so);
        }

        public void Disconnect()
        {
            IsAvailable = false;
        }

        internal void Close()
        {
            IsAvailable = false;
            Socket.Close();
            Socket.Dispose();
        }

        internal void Update()
        {
            TimeoutUpdate();
        }

        private void TimeoutUpdate()
        {
            if (KeepAliveTimeout <= 0) return;
            var now = TimeManager.CurrentTimeInMillis;
            TimeoutHandler(now - LastPacketMillis);
            if (now - LastPacketMillis > KeepAliveTimeout)
                Disconnect();
        }

        /// <summary>
        /// Call on every frame(on available).<br/><br/>
        /// You can do send keep alive packet or others...<br/>
        /// <example>
        /// <code>
        /// class AlivePacket : IPacket { ... }
        /// SendPacket(new AlivePacket());
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="deltaTime">current millis - last packet send millis</param>
        protected virtual void TimeoutHandler(long deltaTime) { }

        private void ReceiveAsync(IAsyncResult rs)
        {
            var so = (rs.AsyncState as StateObject)!;
            try
            {
                var read = so!.TargetSocket.EndReceive(rs);
                if (read > 0)
                {
                    LastPacketMillis = TimeManager.CurrentTimeInMillis;
                    _receiveBuf.Read(RawHandler?.Read(so.Buffer, read) ?? so.Buffer, read);
                    var result = _receiveBuf.ReadPacket();
                    while (result.Length > 0)
                    {
                        PacketHandle(result);
                        result = _receiveBuf.ReadPacket();
                    }

                    so.TargetSocket.BeginReceive(so.Buffer, 0, so.Buffer.Length, 0, ReceiveAsync, so);
                }
            }
            catch (Exception e)
            {
                ExceptionManage(e);
            }
        }

        private void ExceptionManage(Exception e)
        {
            while (e.InnerException != null)
                e = e.InnerException;

            ExceptionHandler(e);
            Disconnect();
        }

        protected virtual void ExceptionHandler(Exception e) { }
        private void PacketHandle(byte[] data)
        {
            if (Compression.CompressionEnabled)
                data = Decompress(data);

            var packet = PacketFactory.Handle(new ByteBuf(data));
            if (packet != null)
            {
                if (PacketHandler == null)
                    _receivePacket.Enqueue(packet);
                else
                    PacketHandler?.Handle(this, packet);
            }
        }

        public void SendPacket(IPacket packet)
        {
            if (!IsAvailable || Socket is not { Connected: true }) return;

            var buf = new ByteBuf();
            buf.WriteVarInt(packet.PacketPrimaryKey);
            packet.Write(buf);

            var data = Compression.CompressionEnabled ? Compress(buf) : buf.Flush();
            try
            {
                Socket.Send(RawHandler?.Write(data) ?? data);
                LastPacketMillis = TimeManager.CurrentTimeInMillis;
            }
            catch (Exception e)
            {
                ExceptionManage(e);
            }
        }

        private byte[] Compress(ByteBuf buf)
        {
            var result = new ByteBuf();
            if (buf.WriteLength >= Compression.CompressionThreshold)
            {
                var compressed = Compressor.Compress(buf.GetBytes());
                result.WriteVarInt(buf.WriteLength);
                result.Write(compressed);
            }
            else
            {
                result.WriteVarInt(0);
                result.Write(buf.GetBytes());
            }
            return result.Flush();
        }

        private byte[] Decompress(byte[] buf)
        {
            var result = new ByteBuf(buf);
            var length = result.ReadVarInt();

            var compressed = result.Read(result.Length);
            return length == 0 ? compressed : Compressor.Decompress(compressed);
        }
    }
}
