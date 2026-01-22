using LiteNetLib;
using UnityEngine;

public class ClientConnection
{
    public EventBasedNetListener Listener { get; private set; }
    private NetManager _client;
    private ClientSender _sender;

    private const string CONNECTION_KEY = "MMO_KEY";

    public ClientConnection(ClientSender sender)
    {
        _sender = sender;
        Listener = new EventBasedNetListener();
        Listener.PeerConnectedEvent += OnConnected;
        Listener.PeerDisconnectedEvent += OnDisconnected;
    }

    public NetManager Connect(string ip, int port, string connectionKey)
    {
        _client = new NetManager(Listener);
        _client.Start();
        _client.Connect(ip, port, connectionKey);
        return _client;
    }

    private void OnConnected(NetPeer peer)
    {
        NetworkLogger.Info($"Connected to server: {peer.Id}");

        // Send Auth
        string token = "TEST_TOKEN"; // Replace with your login token
        _sender.SendAuth(peer, token);
    }

    private void OnDisconnected(NetPeer peer, DisconnectInfo info)
    {
        NetworkLogger.Warning($"Disconnected from server: {info.Reason}");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void Shutdown(string reason)
    {
        NetworkLogger.Warning($"Client shutdown: {reason}");
        _client?.Stop();
        _client = null;
    }
}
