using System.Collections.Concurrent;
using System.Net.Sockets;
using NetworkLibrary.Networks.Packet;
using NetworkLibrary.Utils;

namespace NetworkLibrary.Networks.Multi
{
    public class MultiNetworkClient : IMultiNetworkSocket
    {
        private Type _networkInstance = typeof(MultiNetwork);
        private TemporaryMultiNetwork? _temporaryMultiNetwork;
        public MultiNetwork? Network { get; private set; } = null;
        private PacketFactory _packetFactory;
        public PacketFactory PacketFactory
        {
            get => Network!.PacketFactory;
            private set
            {
                Network!.PacketFactory = value;
            }
        }
        public IMultiPacketHandler? PacketHandler
        {
            get => Network!.PacketHandler;
            set
            {
                Network!.PacketHandler = value;
            }
        }

        public bool IsAvailable => Network?.IsAvailable ?? false;
        public bool IsWaiting { get; private set; } = true;
        private readonly ConcurrentQueue<IPacket> packetStack = new();
        public EventHandler<MultiNetworkEventArgs>? OnConnected;
        public EventHandler<MultiNetworkEventArgs>? OnConnectFailed;
        public EventHandler<MultiNetworkEventArgs>? OnDisconnect;
        public readonly string Host;
        public readonly int Port;
        public readonly AddressFamily Family;
        public readonly SocketType NetworkType;
        public readonly ProtocolType Protocol;
        private readonly int _timeout;
        public readonly int ReceiveBufferSize;

        public MultiNetworkClient(
            PacketFactory packetFactory,
            string host,
            int port,
            AddressFamily family = AddressFamily.InterNetwork,
            SocketType type = SocketType.Stream,
            ProtocolType protocol = ProtocolType.Tcp,
            int timeout = 0,
            int receiveBufferSize = 1024 * 5,
            Type? networkInstance = null)
        {
            _packetFactory = packetFactory;
            Host = host;
            Port = port;
            Family = family;
            NetworkType = type;
            Protocol = protocol;
            _networkInstance = networkInstance ?? typeof(MultiNetwork);
            _temporaryMultiNetwork = new TemporaryMultiNetwork(new Socket(family, type, protocol), this);
            //Network = (Activator.CreateInstance(_networkInstance, new Socket(family, type, protocol)) as Network)!;
            //Network.PacketFactory = packetFactory;
            _timeout = timeout;
            ReceiveBufferSize = receiveBufferSize;
        }

        public void Close()
        {
            Network?.Close();
        }

        public void Connect()
        {
            AsyncConnect(Host, Port, _timeout);
        }

        private async void AsyncConnect(string host, int port, int timeout)
        {
            if (timeout == 0)
                Connect(_temporaryMultiNetwork!.Socket, host, port);
            else
                await ConnectTimeout(_temporaryMultiNetwork!.Socket, host, port, timeout);
            ConnectFinished();
        }

        private void ConnectFinished()
        {
            if (!_temporaryMultiNetwork!.Socket.Connected)
            {
                _temporaryMultiNetwork.Close();
                Network = (Activator.CreateInstance(_networkInstance, _temporaryMultiNetwork.Socket) as MultiNetwork)!;
                OnConnectFailed?.Invoke(this, new MultiNetworkEventArgs(Network));
                return;
            }
            _temporaryMultiNetwork.SocketBounding();
            _temporaryMultiNetwork.Send();
        }

        private void Connect(Socket socket, string host, int port)
        {
            socket.Connect(host, port);
        }

        private Task ConnectTimeout(Socket socket, string host, int port, int timeout)
        {
            return Task.Run(() =>
            {
                var result = socket.BeginConnect(host, port, null, null);
                var connected = result.AsyncWaitHandle.WaitOne(timeout, true);
                try
                {
                    socket.EndConnect(result);
                    if (connected)
                    {
                        return;
                    }
                }
                catch (Exception)
                {
                    // ignored
                }

                return;
            });
        }

        public void SendPacket(IPacket packet)
        {
            if (packet == null) return;
            if (IsWaiting)
            {
                packetStack.Enqueue(packet); return;
            }
            Network!.SendPacket(packet);
        }

        private async void UpdateWorker()
        {
            while (IsAvailable)
            {
                try
                {
                    await Task.Delay(20);
                    if (!Network!.IsAvailable)
                    {
                        OnDisconnect?.Invoke(this, new MultiNetworkEventArgs(Network));
                        Network.Close();
                        break;
                    }
                    if (Network?.Connected ?? false) Network.Update();
                }
                catch (Exception) { }
            }
        }


        void IMultiNetworkSocket.BindConnecting(TemporaryMultiNetwork network) => throw new NotImplementedException();

        void IMultiNetworkSocket.BindConnecting(TemporaryMultiNetwork network, Guid id) => throw new NotImplementedException();

        async void IMultiNetworkSocket.BindConnecting(TemporaryMultiNetwork network, int size, Guid id)
        {
            IsWaiting = false;
            Network = (Activator.CreateInstance(_networkInstance, _temporaryMultiNetwork!.Socket) as MultiNetwork)!;
            Network.SocketID = id;
            Network.PacketFactory = _packetFactory;
            Network.BeginReceive(_temporaryMultiNetwork.Socket, ReceiveBufferSize, network.Buffer);

            _temporaryMultiNetwork!.Close();
            _temporaryMultiNetwork = null;

            for (var i = 1; i < size; i++)
            {
                var socket = new TemporaryMultiNetwork(new Socket(Family, NetworkType, Protocol), this);
                if (_timeout > 0)
                    await ConnectTimeout(socket.Socket, Host, Port, _timeout);
                else
                    Connect(socket.Socket, Host, Port);

                socket.Send(id);
                socket.Close();
                Network.ReceiveChildNetwork(socket.Socket);
                Network.BeginReceive(socket.Socket, ReceiveBufferSize, socket.Buffer);
            }

            while (!packetStack.IsEmpty)
            {
                if (!packetStack.TryDequeue(out var packet)) continue;
                SendPacket(packet);
            }
            OnConnected?.Invoke(this, new MultiNetworkEventArgs(Network));
            UpdateWorker();
        }
    }
}
