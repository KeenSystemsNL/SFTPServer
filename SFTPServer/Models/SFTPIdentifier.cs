namespace SFTP.Models;

public abstract record SFTPIdentifier(uint Id, string Name)
{
    public SFTPIdentifier(uint Id)
        : this(Id, LookupId(Id)) { }

    protected static string LookupId(uint id) => id switch
    {
        0 => "root",
        65534 => "nobody",
        _ => "unknown"
    };
}
