using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using NetworkLibrary.Networks.Packet;
using NetworkLibrary.Utils;

namespace NetworkLibrary.Networks.Multi
{
    public class MultiNetworkListener : IMultiNetworkSocket
    {
        private readonly Socket _socket;
        private Type _networkInstance = typeof(MultiNetwork);
        public bool IsAvailable { get; private set; } = true;
        private readonly ConcurrentBag<MultiNetwork> _networks = [];
        public ReadOnlyCollection<MultiNetwork> Networks => new([.. _networks]);
        public EventHandler<MultiNetworkEventArgs>? OnAcceptEventHandler;
        public EventHandler<MultiNetworkEventArgs>? OnDisconnectEventHandler;
        public PacketFactory DefaultPacketFactory { get; set; }
        public readonly int ReceiveBufferSize;
        public int MultiCount { get; private set; }

        public MultiNetworkListener(
            PacketFactory packetFactory,
            int port,
            int multiCount,
            AddressFamily family = AddressFamily.InterNetwork,
            SocketType type = SocketType.Stream,
            ProtocolType protocol = ProtocolType.Tcp,
            int receiveBufferSize = 1024 * 2
            ) : this(packetFactory, new IPEndPoint(IPAddress.Any, port), multiCount, family, type, protocol, receiveBufferSize) { }
        public MultiNetworkListener(
            PacketFactory packetFactory,
            IPEndPoint ipEndPoint,
            int multiCount,
            AddressFamily family = AddressFamily.InterNetwork,
            SocketType type = SocketType.Stream,
            ProtocolType protocol = ProtocolType.Tcp,
            int receiveBufferSize = 1024 * 2
            )
        {
            ReceiveBufferSize = receiveBufferSize;
            DefaultPacketFactory = packetFactory;
            MultiCount = multiCount;
            _socket = new Socket(family, type, protocol)
            {
                NoDelay = true
            };
            _socket.Bind(ipEndPoint);
        }

        public void Listen(int backlog)
        {
            _socket.Listen(backlog);

            _socket.BeginAccept(AcceptSocket, null);
            UpdateWorker();
        }

        /// <summary>
        /// When Client Connected, Create Network Instance.<br/>
        /// <example>
        /// class constructor must set:
        /// <code>
        /// class ClassName : MultiNetwork {
        ///     public ClassName(Socket socket) : base(socket) { ... }
        /// }
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="instance">Network Type</param>
        public void SetNetworkInstance(Type instance)
        {
            if (instance.IsSubclassOf(typeof(MultiNetwork)))
                _networkInstance = instance;
            else
                throw new ArgumentException("@instance must be Network Type.");
        }

        private void AcceptSocket(IAsyncResult result)
        {
            try
            {
                var socket = _socket.EndAccept(result);
                var network = new TemporaryMultiNetwork(socket, this);
                network.SocketBounding();
            }
            catch (Exception) { }
            try
            {
                _socket.BeginAccept(AcceptSocket, null);
            }
            catch (Exception) { }
        }

        public void Close()
        {
            IsAvailable = false;
            _socket.Close();

            foreach (var manager in _networks)
            {
                manager.Close();
            }
        }

        private async void UpdateWorker()
        {
            while (IsAvailable)
            {
                try
                {
                    await Task.Delay(_networks.IsEmpty ? 800 : 20);
                    var destroy = new Queue<MultiNetwork>();
                    foreach (var manager in _networks)
                    {
                        if (!manager.IsAvailable)
                        {
                            destroy.Enqueue(manager);
                            continue;
                        }
                        manager.Update();
                    }
                    while (destroy.Count > 0)
                    {
                        var network = destroy.Dequeue();
                        OnDisconnectEventHandler?.Invoke(this, new MultiNetworkEventArgs(network));

                        network.Close();
                        _networks.Remove(network);
                    }
                }
                catch (Exception) { }
            }
        }

        public void Broadcast(IPacket packet, MultiNetwork? sender = null)
        {
            foreach (var network in _networks)
            {
                if (network == sender || !(network?.IsAvailable ?? false)) continue;
                network.SendPacket(packet);
            }
        }

        void IMultiNetworkSocket.BindConnecting(TemporaryMultiNetwork network)
        {
            var id = Guid.NewGuid();
            network.Close();

            var multiNetwork = (Activator.CreateInstance(_networkInstance, network.Socket) as MultiNetwork)!;
            multiNetwork.SocketID = id;
            multiNetwork.PacketFactory = DefaultPacketFactory;

            OnAcceptEventHandler?.Invoke(this, new MultiNetworkEventArgs(multiNetwork));
            multiNetwork.BeginReceive(network.Socket, ReceiveBufferSize, network.Buffer);
            _networks.Add(multiNetwork);

            network.Send(MultiCount, id);
        }

        void IMultiNetworkSocket.BindConnecting(TemporaryMultiNetwork socket, Guid id)
        {
            socket.Close();
            foreach (var network in _networks)
            {
                if (network.SocketID == id)
                {
                    network.ReceiveChildNetwork(socket.Socket);
                    network.BeginReceive(socket.Socket, ReceiveBufferSize, socket.Buffer);
                    return;
                }
            }
            socket.Disconnect();
        }

        void IMultiNetworkSocket.BindConnecting(TemporaryMultiNetwork network, int size, Guid id) => throw new NotImplementedException();
    }
}
