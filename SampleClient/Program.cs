// See https://aka.ms/new-console-template for more information

using NetworkLibrary.Networks.Packet;
using SampleClient;
using NetworkLibrary.Networks.Multi;

var client = new MultiNetworkClient(new PacketFactory(), "127.0.0.1", 12345, timeout: 1000);
client.OnConnectFailed += (sender, args) =>
{
    Console.WriteLine("Connect Failed");
};
client.OnConnected += async (sender, args) =>
{
    Console.WriteLine("Connect Success.");
    args.Network!.Compression.CompressionEnabled = true;
    client.PacketFactory.RegisterPacket(new SamplePacket());
    client.SendPacket(new SamplePacket("AAAA"));
    client.SendPacket(new SamplePacket("Hi first connect."));
    client.SendPacket(new SamplePacket("BBB"));
    client.SendPacket(new SamplePacket("CCC"));
    client.SendPacket(new SamplePacket("DDD"));
    client.SendPacket(new SamplePacket("EEE"));
};
client.Connect();
Console.ReadKey();

//NetworkClient client = new(new PacketFactory(), "127.0.0.1", 12345, timeout: 1000);
//client.Network.Compression.CompressionEnabled = true;
//client.OnConnectFailed += (sender, NetworkEventArgs) =>
//{
//    Console.WriteLine("Connect Failed.");
//};
//client.OnConnected += (sender, NetworkEventArgs) =>
//{
//    Console.WriteLine("Connect Success.");
//    client.PacketFactory.RegisterPacket(new SamplePacket());
//    client.SendPacket(new SamplePacket(""));
//    client.SendPacket(new SamplePacket("Hi first connect."));
//};
//client.Connect();
//Console.ReadKey();
