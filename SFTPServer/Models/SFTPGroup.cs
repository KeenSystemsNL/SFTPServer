namespace SFTP.Models;

public record SFTPGroup(uint Id, string Name) : SFTPIdentifier(Id, Name)
{
    public static readonly SFTPGroup Root = new(0);
    public static readonly SFTPGroup Nobody = new(65534);

    public SFTPGroup(uint Id)
        : this(Id, LookupId(Id)) { }
}
