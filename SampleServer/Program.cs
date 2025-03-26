using NetworkLibrary.Networks.Packet;
using NetworkLibrary.Networks;
using SampleServer;
using System.Net;

var listener = new NetworkListener(
    new PacketFactory().RegisterPacket(new SamplePacket()),
    12345
    );
listener.OnAcceptEventHandler += (sender, args) =>
{
    args.network!.PacketHandler = new PacketHandler();
    Console.WriteLine("Client connected: " + (args.network!.Socket.RemoteEndPoint as IPEndPoint));
    Console.WriteLine(string.Join(", ", args.network!.PacketFactory.Packets.Values));
};
listener.OnDisconnectEventHandler += (sender, args) =>
{
    Console.WriteLine("Client disconnected: " + (args.network!.Socket.RemoteEndPoint as IPEndPoint));
};
listener.Listen(20);
Console.WriteLine("Server Opened");
Console.ReadKey();
