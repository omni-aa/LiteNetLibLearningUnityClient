using MessagePack;
using SharedLibrary;

public static class PacketSerializer
{
    public static byte[] Serialize<T>(PacketType type, T packet)
    {
        byte[] body = MessagePackSerializer.Serialize(packet);
        byte[] data = new byte[body.Length + 1];

        data[0] = (byte)type;
        System.Buffer.BlockCopy(body, 0, data, 1, body.Length);

        return data;
    }
}
