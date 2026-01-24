using MessagePack;

public static class AuthPacketHandlers
{
    public static void Register(PacketManager pm)
    {
        pm.Register(PacketType.Welcome, HandleWelcome);
    }

    private static void HandleWelcome(byte[] payload)
    {
        var packet = MessagePackSerializer
            .Deserialize<WelcomePacket>(payload);

        NetworkLogger.Info(
            $"WELCOME TO | ID={packet.PlayerId} | {packet.Message}"
        );
    }
}
