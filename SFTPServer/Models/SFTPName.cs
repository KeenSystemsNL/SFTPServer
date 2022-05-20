namespace SFTP.Models;

public record SFTPName(string Name, Attributes Attributes)
{
    public static SFTPName FromFileSystemInfo(FileSystemInfo fileSystemInfo)
        => new(fileSystemInfo.Name, Attributes.FromFileSystemInfo(fileSystemInfo));

    public static SFTPName FromString(string Name)
        => new(Name, Attributes.Dummy);
}