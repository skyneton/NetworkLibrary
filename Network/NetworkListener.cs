using NetworkLibrary.Network.Packet;
using NetworkLibrary.Utils;
using PatientSignServerService.Networks.Packet;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;

namespace NetworkLibrary.Network
{
    public class NetworkListener
    {
        private readonly Socket _socket;
        private Type _networkManagerInstance = typeof(NetworkManager);
        public bool IsAvailable { get; private set; } = true;
        private readonly ConcurrentBag<NetworkManager> _networks = new();
        public ReadOnlyCollection<NetworkManager> Networks => new(_networks.ToList());
        private readonly Thread _updater;
        public EventHandler<NetworkEventArgs>? OnAcceptEventHandler;
        public EventHandler<NetworkEventArgs>? OnDisconnectEventHandler;
        public PacketFactory DefaultPacketFactory { get; set; }

        public NetworkListener(
            PacketFactory packetFactory,
            int port,
            AddressFamily family = AddressFamily.InterNetwork,
            SocketType type = SocketType.Stream,
            ProtocolType protocol = ProtocolType.Tcp
            ) : this(packetFactory, new IPEndPoint(IPAddress.Any, port), family, type, protocol) { }
        public NetworkListener(
            PacketFactory packetFactory,
            IPEndPoint ipEndPoint,
            AddressFamily family = AddressFamily.InterNetwork,
            SocketType type = SocketType.Stream,
            ProtocolType protocol = ProtocolType.Tcp
            )
        {
            DefaultPacketFactory = packetFactory;
            _socket = new Socket(family, type, protocol)
            {
                NoDelay = true
            };
            _socket.Bind(ipEndPoint);
            _socket.Listen(20);

            _socket.BeginAccept(AcceptSocket, null);
            _updater = new Thread(UpdateWorker);
            _updater.Start();
        }

        /// <summary>
        /// When Client Connected, Create NetworkManager Instance.<br/>
        /// <example>
        /// class constructor must set:
        /// <code>
        /// class ClassName : NetworkManager {
        ///     public ClassName(Socket socket) : base(socket) { ... }
        /// }
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="instance">NetworkManager Type</param>
        public void SetNetworkManagerInstance(Type instance)
        {
            if (instance.IsSubclassOf(typeof(NetworkManager)))
                _networkManagerInstance = instance;
            else
                throw new ArgumentException("@instance must be NetworkManager Type.");
        }

        private void AcceptSocket(IAsyncResult result)
        {
            try
            {
                var socket = _socket.EndAccept(result);
                var manager = (Activator.CreateInstance(_networkManagerInstance, socket) as NetworkManager)!;
                manager.PacketFactory = DefaultPacketFactory;
                _networks.Add(manager);
                OnAcceptEventHandler?.Invoke(this, new NetworkEventArgs(manager));
            }catch(Exception e) {
                Console.WriteLine(e.ToString());
            }
            try
            {
                _socket.BeginAccept(AcceptSocket, null);
            }
            catch(Exception e) {
                Console.WriteLine(e.ToString());
            }
        }

        public void Close()
        {
            IsAvailable = false;
            _socket.Close();
            _updater.Interrupt();

            foreach (var manager in _networks)
            {
                manager.Close();
            }
        }

        private void UpdateWorker()
        {
            while(IsAvailable)
            {
                try
                {
                    Thread.Sleep(_networks.IsEmpty ? 800 : 50);
                    var destroy = new Queue<NetworkManager>();
                    foreach (var manager in _networks)
                    {
                        if(!manager.IsAvailable)
                        {
                            destroy.Enqueue(manager);
                            continue;
                        }
                        manager.Update();
                    }
                    while(destroy.Count > 0)
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

        public void Broadcast(IPacket packet, NetworkManager? sender = null)
        {
            foreach (var network in _networks)
            {
                if (network == sender || !(network?.IsAvailable ?? false)) continue;
                network.SendPacket(packet);
            }
        }
    }
}
