using MessagePack;

[MessagePackObject]
public class PlayerJoinedPacket
{
    [Key(0)]
    public int PlayerId;
}
