using MessagePack;

[MessagePackObject]
public class AuthPacket
{
    [Key(0)]
    public string Token;
}
