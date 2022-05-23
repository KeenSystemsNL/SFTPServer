using SFTP.Enums;
using System.Globalization;

namespace SFTP.Models;

public record SFTPAttributes(
    ulong FileSize,
    SFTPUser User,
    SFTPGroup Group,
    Permissions Permissions,
    DateTimeOffset LastAccessedTime,
    DateTimeOffset LastModifiedTime
)
{
    public static readonly SFTPAttributes DummyFile = new(
        0, SFTPUser.Root, SFTPGroup.Root, Permissions.DefaultFile, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch
    );
    public static readonly SFTPAttributes DummyDirectory = DummyFile with { Permissions = Permissions.DefaultDirectory };

    public IDictionary<string, string> ExtendeAttributes { get; } = new Dictionary<string, string>();
    public string GetLongFileName(string name)
        => ((FormattableString)$"{GetPermissionBits()} {1,3} {User.Name,-8} {Group.Name,-8} {FileSize,8} {LastModifiedTime,12:MMM dd HH:mm} {name}").ToString(CultureInfo.InvariantCulture);

    public static SFTPAttributes FromFileSystemInfo(FileSystemInfo fileSystemInfo)
        => new(
            GetLength(fileSystemInfo),
            SFTPUser.Root,
            SFTPGroup.Root,
            GetFileTypeBits(fileSystemInfo),
            fileSystemInfo.LastAccessTimeUtc,
            fileSystemInfo.LastWriteTimeUtc
        );

    private string GetPermissionBits()
        => $"{(Permissions.HasFlag(Permissions.Directory) ? "d" : "-")}{AttrStr(Permissions.HasFlag(Permissions.UserRead), Permissions.HasFlag(Permissions.UserWrite), Permissions.HasFlag(Permissions.UserExecute))}{AttrStr(Permissions.HasFlag(Permissions.GroupRead), Permissions.HasFlag(Permissions.GroupWrite), Permissions.HasFlag(Permissions.GroupExecute))}{AttrStr(Permissions.HasFlag(Permissions.OtherRead), Permissions.HasFlag(Permissions.OtherWrite), Permissions.HasFlag(Permissions.OtherExecute))}";

    private static string AttrStr(bool read, bool write, bool execute)
        => $"{(read ? "r" : "-")}{(write ? "w" : "-")}{(execute ? "x" : "-")}";

    private static Permissions GetFileTypeBits(FileSystemInfo fsInfo)
        => fsInfo switch
        {
            DirectoryInfo => Permissions.DefaultDirectory,
            FileInfo => Permissions.DefaultFile,
            _ => Permissions.None
        };

    private static ulong GetLength(FileSystemInfo fsInfo)
        => fsInfo switch
        {
            FileInfo => (ulong)((FileInfo)fsInfo).Length,
            _ => 0
        };
}
