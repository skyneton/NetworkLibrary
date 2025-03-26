// See https://aka.ms/new-console-template for more information

using NetworkLibrary.Networks.Packet;
using NetworkLibrary.Networks;
using SampleClient;

NetworkClient client = new(new PacketFactory(), "127.0.0.1", 12345, timeout: 1000);
client.OnConnectFailed += (sender, NetworkEventArgs) =>
{
    Console.WriteLine("Connect Failed.");
};
client.OnConnected += (sender, NetworkEventArgs) =>
{
    Console.WriteLine("Connect Success.");
    client.PacketFactory.RegisterPacket(new SamplePacket());
    client.SendPacket(new SamplePacket("Hi first connect."));
};
client.Connect();
Console.ReadKey();
