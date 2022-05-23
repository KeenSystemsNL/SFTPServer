namespace SFTP.Models;

public record SFTPGroup(uint Id, string Name) : SFTPIdentifier(Id, Name)
{
    public SFTPGroup(uint Id)
        : this(Id, LookupId(Id)) { }
}
