using System;
using MessagePack;
using SharedLibrary;
using UnityEngine;

public static class WorldPacketHandlers
{
    public static void Register(PacketManager pm)
    {
        pm.Register(PacketType.PlayerJoined, HandlePlayerJoined);
        pm.Register(PacketType.MOTD, HandleMOTD);
    }

    private static void HandlePlayerJoined(byte[] payload)
    {
        try
        {
            var packet = MessagePackSerializer.Deserialize<PlayerJoinedPacket>(payload);
            NetworkLogger.Info($"PLAYER JOINED | ID={packet.PlayerId}");
            Debug.Log($"Player joined: ID={packet.PlayerId}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error handling PlayerJoined packet: {ex.Message}");
        }
    }

    private static void HandleMOTD(byte[] payload)
    {
        try
        {
            var packet = MessagePackSerializer.Deserialize<MOTDPacket>(payload);
            NetworkLogger.Info($"MOTD: {packet.Message}");
            Debug.Log($"MOTD received: {packet.Message}");
            ClientEvents.RaiseMOTD(packet.Message);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error handling MOTD packet: {ex.Message}");
        }
    }
}