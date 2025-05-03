using NetworkLibrary.Networks.Packet;
using NetworkLibrary.Networks;
using SampleServer;
using System.Net;
using System.Net.Sockets;

var listener = new NetworkListener(
    new PacketFactory().RegisterPacket(new SamplePacket()),
    12345
    );
listener.SetNetworkInstance(typeof(Test));
listener.OnAcceptEventHandler += (sender, args) =>
{
    args.Network!.PacketHandler = new PacketHandler();
    args.Network!.Compression.CompressionEnabled = true;
    Console.WriteLine("Client connected: " + (args.Network!.Socket.RemoteEndPoint as IPEndPoint));
    Console.WriteLine(string.Join(", ", args.Network!.PacketFactory.Packets.Values));
};
listener.OnDisconnectEventHandler += (sender, args) =>
{
    Console.WriteLine("Client disconnected: " + (args.Network!.Socket.RemoteEndPoint as IPEndPoint));
};
listener.Listen(20);
Console.WriteLine("Server Opened");
Console.ReadKey();

class Test(Socket socket) : Network(socket)
{
    protected override void ExceptionHandler(Exception e)
    {
        Console.WriteLine(e);
    }
}