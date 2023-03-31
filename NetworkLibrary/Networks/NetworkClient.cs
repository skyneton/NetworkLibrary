using NetworkLibrary.Networks.Packet;
using NetworkLibrary.Utils;
using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NetworkLibrary.Networks
{
    public class NetworkClient
    {
        private readonly Type _networkInstance = typeof(Network);
        public Network Network { get; private set; }
        public PacketFactory PacketFactory
        {
            get => Network.PacketFactory;
            set
            {
                Network.PacketFactory = value;
            }
        }
        public bool IsAvailable => Network.IsAvailable;
        public bool IsWaiting = true;
        private readonly ConcurrentQueue<IPacket> packetStack = new();
        public EventHandler<NetworkEventArgs>? OnConnected;
        public EventHandler<NetworkEventArgs>? OnConnectFailed;
        public EventHandler<NetworkEventArgs>? OnDisconnect;
        public readonly string Host;
        public readonly int Port;
        private readonly int _timeout;

        private readonly Thread _updater;

        public NetworkClient(
            PacketFactory packetFactory,
            string host,
            int port,
            AddressFamily family = AddressFamily.InterNetwork,
            SocketType type = SocketType.Stream,
            ProtocolType protocol = ProtocolType.Tcp,
            int timeout = 0,
            Type? networkInstance = null)
        {
            Host = host;
            Port = port;
            _networkInstance = networkInstance ?? typeof(Network);
            Network = (Activator.CreateInstance(_networkInstance, new Socket(family, type, protocol)) as Network)!;
            Network.PacketFactory = packetFactory;
            _timeout = timeout;

            _updater = new Thread(UpdateWorker);
            _updater.Start();
        }

        public void Close()
        {
            Network.Close();
            _updater.Interrupt();
        }

        public void Connect()
        {
            AsyncConnect(Host, Port, _timeout);
        }

        private async void AsyncConnect(string host, int port, int timeout)
        {
            if (timeout == 0)
                Connect(host, port);
            else
                await ConnectTimeout(host, port, timeout);
            ConnectFinished();
        }

        private void ConnectFinished()
        {
            IsWaiting = false;
            if (!Network.Socket.Connected)
            {
                OnConnectFailed?.Invoke(this, new NetworkEventArgs(Network));
                return;
            }
            OnConnected?.Invoke(this, new NetworkEventArgs(Network));
            Network.BeginReceive();
        }

        private void Connect(string host, int port)
        {
            Network.Socket.Connect(host, port);
        }

        private Task ConnectTimeout(string host, int port, int timeout)
        {
            return Task.Run(() =>
            {
                var result = Network.Socket.BeginConnect(host, port, null, null);
                var connected = result.AsyncWaitHandle.WaitOne(timeout, true);
                try
                {
                    Network.Socket.EndConnect(result);
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
            if(IsWaiting)
            {
                packetStack.Enqueue(packet); return;
            }
            Network.SendPacket(packet);
        }

        private void UpdateWorker()
        {
            while(IsAvailable)
            {
                try
                {
                    Thread.Sleep(50);
                    if (!Network.IsAvailable)
                    {
                        OnDisconnect?.Invoke(this, new NetworkEventArgs(Network));
                        Network.Close();
                        break;
                    }
                    if(Network.Socket?.Connected ?? false) Network.Update();
                }
                catch (Exception) { }
            }
        }
    }
}
