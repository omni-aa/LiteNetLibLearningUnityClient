using System;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using LiteNetLib;
using SharedLibrary;
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
        
        // Test encryption/decryption locally first
        TestEncryptionDecryption();
        
        Client.Connect(SERVER_IP, SERVER_PORT, "MMO_KEY");

        Debug.Log("Client started");
    }
    
    private void RegisterPackets()
    {
        AuthPacketHandlers.Register(packetManager);
        WorldPacketHandlers.Register(packetManager);
    }

    private void TestEncryptionDecryption()
    {
        Debug.Log("=== TESTING ENCRYPTION/DECRYPTION ===");
        
        try
        {
            // Test data similar to what we send
            var testPacket = new AuthPacket { Token = "test-token-123" };
            byte[] testData = PacketManager.Serialize(PacketType.Auth, testPacket);
            
            Debug.Log($"Test data length: {testData.Length}");
            Debug.Log($"Test data (hex): {BitConverter.ToString(testData)}");
            Debug.Log($"First byte (type): {testData[0]} = {(PacketType)testData[0]}");
            
            // Encrypt and sign
            byte[] encryptedSigned = Shared.PacketSecurity.EncryptAndSign(testData);
            Debug.Log($"Encrypted+Signed length: {encryptedSigned.Length}");
            
            // Extract HMAC and encrypted data
            byte[] hmac = new byte[32];
            byte[] encryptedData = new byte[encryptedSigned.Length - 32];
            Array.Copy(encryptedSigned, 0, hmac, 0, 32);
            Array.Copy(encryptedSigned, 32, encryptedData, 0, encryptedData.Length);
            
            Debug.Log($"HMAC (first 8): {BitConverter.ToString(hmac, 0, 8)}...");
            Debug.Log($"Encrypted data (first 16): {BitConverter.ToString(encryptedData, 0, Math.Min(16, encryptedData.Length))}...");
            
            // Verify
            bool valid = Shared.PacketSecurity.Verify(encryptedData, hmac);
            Debug.Log($"HMAC valid locally: {valid}");
            
            if (!valid)
            {
                Debug.LogError("Local HMAC test failed!");
                return;
            }
            
            // Decrypt
            byte[] decrypted = Shared.PacketSecurity.Decrypt(encryptedData);
            Debug.Log($"Decrypted length: {decrypted.Length}");
            Debug.Log($"Decrypted (hex): {BitConverter.ToString(decrypted)}");
            
            if (decrypted.Length > 0)
            {
                Debug.Log($"First byte after decrypt: {decrypted[0]} = {(PacketType)decrypted[0]}");
                
                // Compare with original
                bool dataMatches = StructuralComparisons.StructuralEqualityComparer.Equals(testData, decrypted);
                Debug.Log($"Decrypted matches original: {dataMatches}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Encryption/Decryption test failed: {ex.Message}");
        }
        
        Debug.Log("=== END TEST ===\n");
    }

    private void OnConnected(NetPeer peer)
    {
        Debug.Log($"Connected to server: {peer}");
        sender.SendAuth(peer, START_TOKEN); // send token to GameServer
    }

    private void OnDisconnected(NetPeer peer, DisconnectInfo info)
    {
        Debug.LogWarning($"Disconnected from {peer}: {info.Reason}");
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

        Debug.Log($"=== PACKET RECEIVED FROM SERVER ===");
        Debug.Log($"Raw data length: {data.Length} bytes");

        if (data.Length < 32)
        {
            Debug.LogError($"Data too short: {data.Length} bytes (need at least 32 for HMAC)");
            return;
        }

        byte[] hmac = new byte[32];
        byte[] encrypted = new byte[data.Length - 32];
        Array.Copy(data, 0, hmac, 0, 32);
        Array.Copy(data, 32, encrypted, 0, encrypted.Length);

        Debug.Log($"HMAC (first 8): {BitConverter.ToString(hmac, 0, 8)}...");
        Debug.Log($"Encrypted length: {encrypted.Length} bytes");
        
        if (encrypted.Length > 0)
        {
            Debug.Log($"Encrypted (first 16): {BitConverter.ToString(encrypted, 0, Math.Min(16, encrypted.Length))}");
        }

        // Verify HMAC
        bool hmacValid = Shared.PacketSecurity.Verify(encrypted, hmac);
        Debug.Log($"HMAC valid: {hmacValid}");
        
        if (!hmacValid)
        {
            Debug.LogError("HMAC verification failed!");
            
            // Debug: Compute expected HMAC
            try
            {
                using var hmacAlg = new HMACSHA256(Encoding.UTF8.GetBytes("12345678901234567890123456789012"));
                byte[] expectedHmac = hmacAlg.ComputeHash(encrypted);
                Debug.Log($"Expected HMAC (first 8): {BitConverter.ToString(expectedHmac, 0, 8)}...");
                Debug.Log($"Received HMAC (first 8): {BitConverter.ToString(hmac, 0, 8)}...");
            }
            catch (Exception ex) 
            { 
                Debug.LogError($"HMAC debug failed: {ex.Message}");
            }
            
            return;
        }

        // Decrypt
        byte[] decrypted;
        try
        {
            decrypted = Shared.PacketSecurity.Decrypt(encrypted);
            Debug.Log($"Decrypted length: {decrypted.Length} bytes");
            
            if (decrypted.Length > 0)
            {
                Debug.Log($"Decrypted (hex): {BitConverter.ToString(decrypted)}");
                Debug.Log($"First byte as int: {decrypted[0]}");
            }
            else
            {
                Debug.LogError("Decrypted data is empty!");
                return;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Decryption failed: {ex.Message}");
            Debug.LogError($"Stack trace: {ex.StackTrace}");
            return;
        }

        // Extract packet type
        if (decrypted.Length < 1)
        {
            Debug.LogError("Decrypted data has no packet type byte!");
            return;
        }

        PacketType type = (PacketType)decrypted[0];
        Debug.Log($"Packet Type: {type} (byte value: {(byte)type})");

        byte[] payload = new byte[decrypted.Length - 1];
        Array.Copy(decrypted, 1, payload, 0, payload.Length);
        Debug.Log($"Payload length: {payload.Length} bytes");

        // Handle the packet
        packetManager.Handle(type, payload);
        Debug.Log($"=== END PACKET ===\n");
    }

    void Update() => Client?.PollEvents();
    
    void OnDestroy() 
    { 
        Client?.Stop();
        Debug.Log("ClientNetwork destroyed");
    }
}