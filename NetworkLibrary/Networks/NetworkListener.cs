using NetworkLibrary.Networks.Packet;
using NetworkLibrary.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace NetworkLibrary.Networks
{
    public class NetworkListener
    {
        private readonly Socket _socket;
        private Type _networkInstance = typeof(Network);
        public bool IsAvailable { get; private set; } = true;
        private readonly ConcurrentBag<Network> _networks = new();
        public ReadOnlyCollection<Network> Networks => new(_networks.ToList());
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
            System.Net.IPEndPoint ipEndPoint,
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
            _updater = new Thread(UpdateWorker);
        }

        public void Listen(int backlog)
        {
            _socket.Listen(backlog);

            _socket.BeginAccept(AcceptSocket, null);
            _updater.Start();
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
                network.BeginReceive();
                _networks.Add(network);
            }
            catch(Exception) { }
            try
            {
                _socket.BeginAccept(AcceptSocket, null);
            }
            catch(Exception) { }
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
                    var destroy = new Queue<Network>();
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
