# NetworkLib.
## How To Use?
### Packet Example
```csharp
using NetworkLibrary.Networks.Packet;
using NetworkLibrary.Utils;

public class ExamplePacket : IPacket
{
    //Packet distinguishing ID.
    public int PacketPrimaryKey => {packet ID};
    public string Data { get; private set; } = string.Empty;

    //You must create non-parameter constructor.
    public ExamplePacket() {}
    public ExamplePacket(string data)
    {
        Data = data;
    }

    public void Read(ByteBuf buf)
    {
        Data = buf.ReadString();
    }

    public void Write(ByteBuf buf)
    {
        buf.WriteString(Data);
    }
}
```
### Server Listener Example
```csharp
var listener = new NetworkListener(
    new PacketFactory()
        .RegisterPacket(new ExamplePacket()), //Can register multiple packets.
    {your port}
);

// You can initialize custom Network class.(optional)
listener.SetNetworkInstance(typeof(Your network));

listener.OnAcceptEventHandler += (sender, args) =>
{
    args.network!.PacketHandler = new PacketHandler();
    // You can do set other packet factory and others..(optional)
    args.network!.PacketFactory = new PacketFactory()
        .RegisterPacket(new ExamplePacket());

    Console.WriteLine("Client connected: " + (args.network!.Socket.RemoteEndPoint as IPEndPoint));
};

listener.Listen(20);
```

### Client Example
```csharp
var client = new NetworkClient(
    new PacketFactory()
        .RegisterPacket(new ExamplePacket()),
        "your host",
        {your port},
        timeout: {connect timeout} //optional, if you set, connecting async.
    );
client.OnConnected += (sender, args) =>
{
    // connected.
};
client.OnConnectFailed += (sender, args) =>
{
    // connect failed.
};
client.Connect();
```
![sample_server](https://user-images.githubusercontent.com/67633420/229147583-fc22ca6d-4d22-461f-8413-0d0f4407f6aa.png)
![sample_client](https://user-images.githubusercontent.com/67633420/229147610-d4fb5bb4-586c-4f72-82cc-cf0d5fc21e1a.png)


