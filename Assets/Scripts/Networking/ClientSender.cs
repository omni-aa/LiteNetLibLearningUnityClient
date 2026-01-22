using LiteNetLib;
using Shared;

public class ClientSender
{
    public void SendAuth(NetPeer peer, string token)
    {
        var packet = new AuthPacket { Token = token };
        SendSecure(peer, PacketType.Auth, packet);
    }

    public void SendSecure(NetPeer peer, PacketType type, object packet)
    {
        byte[] data = PacketManager.Serialize(type, packet);
        byte[] encrypted = PacketSecurity.Encrypt(data);
        byte[] hmac = PacketSecurity.HMAC(encrypted);

        byte[] finalData = new byte[hmac.Length + encrypted.Length];
        System.Buffer.BlockCopy(hmac, 0, finalData, 0, hmac.Length);
        System.Buffer.BlockCopy(encrypted, 0, finalData, hmac.Length, encrypted.Length);

        peer.Send(finalData, LiteNetLib.DeliveryMethod.ReliableOrdered);
        NetworkLogger.Sent(type);
    }
}
