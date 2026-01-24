using System;
using MessagePack;
using SharedLibrary;
using UnityEngine;

public static class AuthPacketHandlers
{
    public static void Register(PacketManager pm)
    {
        pm.Register(PacketType.Welcome, HandleWelcome);
    }

    private static void HandleWelcome(byte[] payload)
    {
        try
        {
            var packet = MessagePackSerializer
                .Deserialize<WelcomePacket>(payload);

            NetworkLogger.Info(
                $"WELCOME TO SERVER | ID={packet.PlayerId} | {packet.Message}"
            );
            
            Debug.Log($"Welcome packet received. Player ID: {packet.PlayerId}, Message: {packet.Message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error handling Welcome packet: {ex.Message}");
        }
    }
}