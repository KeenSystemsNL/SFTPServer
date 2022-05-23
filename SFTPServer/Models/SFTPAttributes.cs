using SFTP.Enums;
using System.Globalization;

namespace SFTP.Models;

public record SFTPAttributes(
    ulong FileSize,
    uint Uid,
    uint Gid,
    Permissions Permissions,
    DateTimeOffset LastAccessedTime,
    DateTimeOffset LastModifiedTime
)
{
    private const uint _defaultowner = 0;
    private const uint _defaultgroup = _defaultowner;

    private static readonly Permissions _defaultpermissions =
        Permissions.UserExecute
        | Permissions.UserRead
        | Permissions.UserWrite
        | Permissions.GroupRead;

    public static readonly SFTPAttributes DummyFile = new(
        0, _defaultowner, _defaultgroup, _defaultpermissions, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch
    );
    public static readonly SFTPAttributes DummyDirectory = DummyFile with { Permissions = _defaultpermissions | Permissions.Directory };

    public IDictionary<string, string> ExtendeAttributes { get; } = new Dictionary<string, string>();
    public string GetLongFileName(string name)
        => ((FormattableString)$"{GetPermissionBits()} {1,3} {LookupId(Uid),-8} {LookupId(Gid),-8} {FileSize,8} {LastModifiedTime,12:MMM dd HH:mm} {name}").ToString(CultureInfo.InvariantCulture);

    public static SFTPAttributes FromFileSystemInfo(FileSystemInfo fileSystemInfo)
        => new(
            GetLength(fileSystemInfo),
            _defaultowner,
            _defaultgroup,
            _defaultpermissions | GetFileTypeBits(fileSystemInfo),
            fileSystemInfo.LastAccessTimeUtc,
            fileSystemInfo.LastWriteTimeUtc
        );

    private static string LookupId(uint id) => id switch
    {
        0 => "root",
        65534 => "nobody",
        _ => "unknown"
    };

    private string GetPermissionBits()
        => $"{(Permissions.HasFlag(Permissions.Directory) ? "d" : "-")}{AttrStr(Permissions.HasFlag(Permissions.UserRead), Permissions.HasFlag(Permissions.UserWrite), Permissions.HasFlag(Permissions.UserExecute))}{AttrStr(Permissions.HasFlag(Permissions.GroupRead), Permissions.HasFlag(Permissions.GroupWrite), Permissions.HasFlag(Permissions.GroupExecute))}{AttrStr(Permissions.HasFlag(Permissions.OtherRead), Permissions.HasFlag(Permissions.OtherWrite), Permissions.HasFlag(Permissions.OtherExecute))}";

    private static string AttrStr(bool read, bool write, bool execute)
        => $"{(read ? "r" : "-")}{(write ? "w" : "-")}{(execute ? "x" : "-")}";

    private static Permissions GetFileTypeBits(FileSystemInfo fsInfo)
        => fsInfo switch
        {
            DirectoryInfo => Permissions.Directory,
            FileInfo => Permissions.RegularFile,
            _ => Permissions.None
        };

    private static ulong GetLength(FileSystemInfo fsInfo)
        => fsInfo switch
        {
            FileInfo => (ulong)((FileInfo)fsInfo).Length,
            _ => 0
        };
}
