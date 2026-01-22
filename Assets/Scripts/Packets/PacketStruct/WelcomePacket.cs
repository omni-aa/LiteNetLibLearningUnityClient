using MessagePack;

[MessagePackObject]
public class WelcomePacket
{
    [Key(0)]
    public int PlayerId;

    [Key(1)]
    public string Message;
}
