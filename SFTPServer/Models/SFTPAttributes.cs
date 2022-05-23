using SFTP.Enums;
using System.Globalization;

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

public record User(uint Id, string Name) : SFTPIdentifier(Id, Name)
{
    public User(uint Id)
        : this(Id, LookupId(Id)) { }
}

public record Group(uint Id, string Name) : SFTPIdentifier(Id, Name)
{
    public Group(uint Id)
        : this(Id, LookupId(Id)) { }
}


public record SFTPAttributes(
    ulong FileSize,
    User User,
    Group Group,
    Permissions Permissions,
    DateTimeOffset LastAccessedTime,
    DateTimeOffset LastModifiedTime
)
{
    private static readonly User _defaultowner = new(0);
    private static readonly Group _defaultgroup = new(0);

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
        => ((FormattableString)$"{GetPermissionBits()} {1,3} {User.Name,-8} {Group.Name,-8} {FileSize,8} {LastModifiedTime,12:MMM dd HH:mm} {name}").ToString(CultureInfo.InvariantCulture);

    public static SFTPAttributes FromFileSystemInfo(FileSystemInfo fileSystemInfo)
        => new(
            GetLength(fileSystemInfo),
            _defaultowner,
            _defaultgroup,
            _defaultpermissions | GetFileTypeBits(fileSystemInfo),
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
