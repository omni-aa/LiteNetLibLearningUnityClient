using MessagePack;

public static class WorldPacketHandlers
{
    public static void Register(PacketManager pm)
    {
        pm.Register(PacketType.PlayerJoined, HandlePlayerJoined);
        pm.Register(PacketType.MOTD, HandleMOTD);
    }

    private static void HandlePlayerJoined(byte[] payload)
    {
        var packet = MessagePackSerializer.Deserialize<PlayerJoinedPacket>(payload);
        NetworkLogger.Info($"PLAYER JOINED | ID={packet.PlayerId}");
    }

    private static void HandleMOTD(byte[] payload)
    {
        var packet = MessagePackSerializer.Deserialize<MOTDPacket>(payload);
        NetworkLogger.Info($"MOTD: {packet.Message}");
        ClientEvents.RaiseMOTD(packet.Message);
    }
}
