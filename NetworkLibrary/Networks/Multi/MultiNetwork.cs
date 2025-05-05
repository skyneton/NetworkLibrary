using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Net.Sockets;
using NetworkLibrary.Networks.Compress;
using NetworkLibrary.Networks.Packet;
using NetworkLibrary.Utils;

namespace NetworkLibrary.Networks.Multi
{
    public class MultiNetwork
    {
        private readonly List<Socket> _sockets;
        public ReadOnlyCollection<Socket> Sockets => new([.. _sockets]);
        public readonly PacketCompression Compression;
        public bool Connected => _sockets.First(socket => socket.Connected)?.Connected ?? false;
        public bool IsAvailable { get; private set; }
        /// <summary>
        /// Socket retention time. Do not check if 0.<br/>
        /// If using this, safe set > 800 or set > 50.<br/>
        /// Because frame update every 50 millis and first update 800 millis.
        /// </summary>
        public long KeepAliveTimeout = 0;
        public long LastPacketMillis { get; private set; } = TimeManager.CurrentTimeInMillis;

        /// <summary>
        /// When created instance, initalized DefaultPacketFactory(On Listener) or linked client packetFactory.
        /// </summary>
        public PacketFactory PacketFactory { get; set; }


        /// <summary>
        /// Final/First Byte IO
        /// </summary>
        public IRawHandler? RawHandler { get; set; }
        public ICompressor Compressor = new ZlibCompressor();
        private IMultiPacketHandler? _packetHandler;
        public IMultiPacketHandler? PacketHandler
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
        public Guid SocketID { get; internal set; }
        private int _packetIdx = 0;
        private readonly object _packetIdxLock = new();

        public MultiNetwork(Socket socket)
        {
            socket.NoDelay = true;
            _sockets = [socket];

            IsAvailable = true;
            Compression = new PacketCompression();
        }

        internal void BeginReceive(Socket socket, int bufferSize, NetworkBuf origin)
        {
            var so = new MultiStateObject(socket, bufferSize);
            so.NetworkBuffer.Read(origin.Buffer, origin.WritedLength);
            so.TargetSocket.BeginReceive(so.Buffer, 0, so.Buffer.Length, SocketFlags.None, ReceiveAsync, so);
        }

        public void ReceiveChildNetwork(Socket socket)
        {
            lock (_sockets)
            {
                _sockets.Add(socket);
            }
        }

        public void Disconnect()
        {
            IsAvailable = false;
        }

        internal void Close()
        {
            IsAvailable = false;
            foreach (var socket in _sockets)
            {
                socket.Close();
                socket.Dispose();
            }
            _sockets.Clear();
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
            var so = (rs.AsyncState as MultiStateObject)!;
            try
            {
                var read = so!.TargetSocket.EndReceive(rs);
                if (read > 0)
                {
                    LastPacketMillis = TimeManager.CurrentTimeInMillis;
                    so.NetworkBuffer.Read(RawHandler?.Read(so.Buffer, read) ?? so.Buffer, read);
                    var result = so.NetworkBuffer.ReadPacket();
                    while (result.Length > 0)
                    {
                        PacketHandle(result);
                        result = so.NetworkBuffer.ReadPacket();
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
            var socket = GetSocket();
            if (!IsAvailable || socket is not { Connected: true }) return;

            var buf = new ByteBuf();
            buf.WriteVarInt(packet.PacketPrimaryKey);
            packet.Write(buf);

            var data = Compression.CompressionEnabled ? Compress(buf) : buf.Flush();
            try
            {
                socket.Send(RawHandler?.Write(data) ?? data);
                LastPacketMillis = TimeManager.CurrentTimeInMillis;
            }
            catch (Exception e)
            {
                ExceptionManage(e);
            }
        }

        private Socket? GetSocket()
        {
            if (_sockets.Count == 0) return null;
            int key;
            lock (_packetIdxLock)
            {
                key = ++_packetIdx % _sockets.Count;
                _packetIdx = key;
            }
            var socket = _sockets.ElementAtOrDefault(key);
            if (socket is not { Connected: true })
            {
                RemoveSocket(socket);
                socket = null;
            }
            return socket ?? GetSocket();
        }

        private void RemoveSocket(Socket socket)
        {
            lock (_sockets)
            {
                _sockets.Remove(socket);
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
