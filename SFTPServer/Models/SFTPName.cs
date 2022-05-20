namespace SFTPTest.Models;

public record SFTPName(string Name, SFTPAttributes Attributes)
{
    public static SFTPName FromFileSystemInfo(FileSystemInfo fileSystemInfo)
        => new(fileSystemInfo.Name, SFTPAttributes.FromFileSystemInfo(fileSystemInfo));

    public static SFTPName FromString(string Name)
        => new(Name, SFTPAttributes.Dummy);
}