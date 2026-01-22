using UnityEngine;
using LiteNetLib;

public class ClientNetwork : MonoBehaviour
{
    public static ClientNetwork Instance { get; private set; }

    public NetManager Client { get; private set; }
    private PacketManager packetManager;
    private ClientSender sender;

    private const string SERVER_IP = "127.0.0.1";
    private const int SERVER_PORT = 9050;
    private const string CONNECTION_KEY = "MMO_KEY";

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        packetManager = new PacketManager();
        sender = new ClientSender();

        var listener = new EventBasedNetListener();
        Client = new NetManager(listener);

        listener.PeerConnectedEvent += OnConnected;
        listener.PeerDisconnectedEvent += OnDisconnected;
        listener.NetworkReceiveEvent += OnReceive;

        RegisterPackets();

        Client.Start();
        Client.Connect(SERVER_IP, SERVER_PORT, CONNECTION_KEY);
        NetworkLogger.Info("Client started");
    }

    void Update() => Client?.PollEvents();

    private void RegisterPackets()
    {
        packetManager.Register(PacketType.Welcome, payload =>
        {
            var packet = MessagePack.MessagePackSerializer.Deserialize<WelcomePacket>(payload);
            NetworkLogger.Info($"WELCOME | ID={packet.PlayerId} | {packet.Message}");
        });

        packetManager.Register(PacketType.PlayerJoined, payload =>
        {
            var packet = MessagePack.MessagePackSerializer.Deserialize<PlayerJoinedPacket>(payload);
            NetworkLogger.Info($"PLAYER JOINED | ID={packet.PlayerId}");
        });

        packetManager.Register(PacketType.MOTD, payload =>
        {
            var packet = MessagePack.MessagePackSerializer.Deserialize<MOTDPacket>(payload);
            NetworkLogger.Info($"MOTD: {packet.Message}");
        });
    }

    private void OnConnected(NetPeer peer)
    {
        NetworkLogger.Info($"Connected to server: {peer.Id}");
        sender.SendAuth(peer, "TEST_TOKEN"); // example token
    }

    private void OnDisconnected(NetPeer peer, DisconnectInfo info)
    {
        NetworkLogger.Warning($"Disconnected: {info.Reason}");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnReceive(NetPeer peer, LiteNetLib.NetPacketReader reader, byte channel, LiteNetLib.DeliveryMethod method)
    {
        byte[] receivedData = reader.GetRemainingBytes();
        reader.Recycle();

        if (receivedData.Length < 32) { NetworkLogger.Warning("Packet too short"); return; }

        byte[] hmac = new byte[32];
        byte[] encrypted = new byte[receivedData.Length - 32];
        System.Array.Copy(receivedData, 0, hmac, 0, 32);
        System.Array.Copy(receivedData, 32, encrypted, 0, encrypted.Length);

        if (!Shared.PacketSecurity.Verify(encrypted, hmac))
        {
            NetworkLogger.Warning("HMAC verification failed");
            return;
        }

        byte[] decrypted = Shared.PacketSecurity.Decrypt(encrypted);

        PacketType type = (PacketType)decrypted[0];
        byte[] payload = new byte[decrypted.Length - 1];
        System.Array.Copy(decrypted, 1, payload, 0, payload.Length);

        NetworkLogger.Received(type);
        packetManager.Handle(type, payload);
    }

    void OnDestroy() => Shutdown("Client destroyed");

    private void Shutdown(string reason)
    {
        NetworkLogger.Warning($"Shutting down: {reason}");
        Client?.Stop();
    }
}
