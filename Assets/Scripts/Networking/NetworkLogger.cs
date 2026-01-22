using UnityEngine;

public static class NetworkLogger
{
    public static void Info(string message) => Debug.Log("[Network] " + message);
    public static void Warning(string message) => Debug.LogWarning("[Network] " + message);

    public static void Sent(PacketType type) => Debug.Log($"[Network] Sent packet: {type}");
    public static void Received(PacketType type) => Debug.Log($"[Network] Received packet: {type}");
}
