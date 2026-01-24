using System;
using LiteNetLib;
using UnityEngine;

public class ClientNetwork : MonoBehaviour
{
    public static ClientNetwork Instance { get; private set; }

    public NetManager Client { get; private set; }
    private EventBasedNetListener listener;
    private PacketManager packetManager;
    private ClientSender sender;

    private string SERVER_IP = "127.0.0.1";
    private int SERVER_PORT = 9050;
    private string START_TOKEN = "";

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Read args from GameLauncher
        string[] args = Environment.GetCommandLineArgs();
        foreach (var arg in args)
        {
            if (arg.StartsWith("--token")) START_TOKEN = arg.Split('=')[1].Trim('"');
            if (arg.StartsWith("--ip")) SERVER_IP = arg.Split('=')[1];
            if (arg.StartsWith("--port")) SERVER_PORT = int.Parse(arg.Split('=')[1]);
        }

        Debug.Log($"ClientNetwork Init | Token={START_TOKEN} | IP={SERVER_IP} | Port={SERVER_PORT}");
    }

    void Start()
    {
        packetManager = new PacketManager();
        sender = new ClientSender();

        listener = new EventBasedNetListener();
        Client = new NetManager(listener);

        listener.PeerConnectedEvent += OnConnected;
        listener.PeerDisconnectedEvent += OnDisconnected;
        listener.NetworkReceiveEvent += OnReceive;
        RegisterPackets();
        Client.Start();
        Client.Connect(SERVER_IP, SERVER_PORT, "MMO_KEY");

        Debug.Log("Client started");
    }
    
    private void RegisterPackets()
    {
        AuthPacketHandlers.Register(packetManager);
        WorldPacketHandlers.Register(packetManager);
    }


    private void OnConnected(NetPeer peer)
    {
        Debug.Log($"Connected to server: {peer.Id}");
        sender.SendAuth(peer, START_TOKEN); // send token to GameServer
    }

    private void OnDisconnected(NetPeer peer, DisconnectInfo info)
    {
        Debug.LogWarning($"Disconnected: {info.Reason}");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod method)
    {
        byte[] data = reader.GetRemainingBytes();
        reader.Recycle();

        if (data.Length < 32) return;

        byte[] hmac = new byte[32];
        byte[] encrypted = new byte[data.Length - 32];
        Array.Copy(data, 0, hmac, 0, 32);
        Array.Copy(data, 32, encrypted, 0, encrypted.Length);

        if (!Shared.PacketSecurity.Verify(encrypted, hmac)) return;

        byte[] decrypted = Shared.PacketSecurity.Decrypt(encrypted);
        PacketType type = (PacketType)decrypted[0];
        byte[] payload = new byte[decrypted.Length - 1];
        Array.Copy(decrypted, 1, payload, 0, payload.Length);

        packetManager.Handle(type, payload);
    }

    void Update() => Client?.PollEvents();
    void OnDestroy() => Client?.Stop();
}
