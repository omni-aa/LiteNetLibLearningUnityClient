using System;
using System.Collections.Generic;
using MessagePack;

public class PacketManager
{
    public delegate void Handler(byte[] payload);
    private readonly Dictionary<PacketType, Handler> _handlers = new();

    public void Register(PacketType type, Handler handler)
    {
        _handlers[type] = handler;
    }

    public void Handle(PacketType type, byte[] payload)
    {
        if (!_handlers.TryGetValue(type, out var handler))
        {
            NetworkLogger.Warning($"Unhandled packet: {type}");
            return;
        }
        handler(payload);
    }

    public static byte[] Serialize<T>(PacketType type, T packet)
    {
        byte[] body = MessagePackSerializer.Serialize(packet);
        byte[] data = new byte[body.Length + 1];
        data[0] = (byte)type;
        Array.Copy(body, 0, data, 1, body.Length);
        return data;
    }
}
