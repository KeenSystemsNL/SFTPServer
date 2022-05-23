namespace SFTP.Models;

public record SFTPUser(uint Id, string Name) : SFTPIdentifier(Id, Name)
{
    public static readonly SFTPUser Root = new(0);
    public static readonly SFTPUser Nobody = new(65534);

    public SFTPUser(uint Id)
        : this(Id, LookupId(Id)) { }
}
