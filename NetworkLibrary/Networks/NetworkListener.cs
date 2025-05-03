using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using NetworkLibrary.Networks.Packet;
using NetworkLibrary.Utils;

namespace NetworkLibrary.Networks
{
    public class NetworkListener
    {
        private readonly Socket _socket;
        private Type _networkInstance = typeof(Network);
        public bool IsAvailable { get; private set; } = true;
        private readonly ConcurrentBag<Network> _networks = [];
        public ReadOnlyCollection<Network> Networks => new([.. _networks]);
        public EventHandler<NetworkEventArgs>? OnAcceptEventHandler;
        public EventHandler<NetworkEventArgs>? OnDisconnectEventHandler;
        public PacketFactory DefaultPacketFactory { get; set; }
        public readonly int ReceiveBufferSize;

        public NetworkListener(
            PacketFactory packetFactory,
            int port,
            AddressFamily family = AddressFamily.InterNetwork,
            SocketType type = SocketType.Stream,
            ProtocolType protocol = ProtocolType.Tcp,
            int receiveBufferSize = 1024 * 5
            ) : this(packetFactory, new IPEndPoint(IPAddress.Any, port), family, type, protocol, receiveBufferSize) { }
        public NetworkListener(
            PacketFactory packetFactory,
            IPEndPoint ipEndPoint,
            AddressFamily family = AddressFamily.InterNetwork,
            SocketType type = SocketType.Stream,
            ProtocolType protocol = ProtocolType.Tcp,
            int receiveBufferSize = 1024 * 5
            )
        {
            ReceiveBufferSize = receiveBufferSize;
            DefaultPacketFactory = packetFactory;
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
        /// class ClassName : Network {
        ///     public ClassName(Socket socket) : base(socket) { ... }
        /// }
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="instance">Network Type</param>
        public void SetNetworkInstance(Type instance)
        {
            if (instance.IsSubclassOf(typeof(Network)))
                _networkInstance = instance;
            else
                throw new ArgumentException("@instance must be Network Type.");
        }

        private void AcceptSocket(IAsyncResult result)
        {
            try
            {
                var socket = _socket.EndAccept(result);
                var network = (Activator.CreateInstance(_networkInstance, socket) as Network)!;
                network.PacketFactory = DefaultPacketFactory;
                OnAcceptEventHandler?.Invoke(this, new NetworkEventArgs(network));
                network.BeginReceive(ReceiveBufferSize);
                _networks.Add(network);
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

        private async Task UpdateWorker()
        {
            while (IsAvailable)
            {
                try
                {
                    await Task.Delay(_networks.IsEmpty ? 800 : 20);
                    var destroy = new Queue<Network>();
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
                        OnDisconnectEventHandler?.Invoke(this, new NetworkEventArgs(network));

                        network.Close();
                        _networks.Remove(network);
                    }
                }
                catch (Exception) { }
            }
        }

        public void Broadcast(IPacket packet, Network? sender = null)
        {
            foreach (var network in _networks)
            {
                if (network == sender || !(network?.IsAvailable ?? false)) continue;
                network.SendPacket(packet);
            }
        }
    }
}
