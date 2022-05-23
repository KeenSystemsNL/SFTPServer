namespace SFTP.Models;

public record SFTPUser(uint Id, string Name) : SFTPIdentifier(Id, Name)
{
    public SFTPUser(uint Id)
        : this(Id, LookupId(Id)) { }
}
