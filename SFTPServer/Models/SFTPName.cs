namespace SFTP.Models;

public record SFTPName(string Name, SFTPAttributes Attributes)
{
    public static SFTPName FromFileSystemInfo(FileSystemInfo fileSystemInfo)
        => new(fileSystemInfo.Name, SFTPAttributes.FromFileSystemInfo(fileSystemInfo));

    public static SFTPName FromString(string Name, bool IsDirectory = false)
        => new(Name, IsDirectory ? SFTPAttributes.DummyDirectory : SFTPAttributes.DummyFile);
}