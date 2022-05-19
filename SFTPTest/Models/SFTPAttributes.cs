using SFTPTest.Enums;
using System.Globalization;

namespace SFTPTest.Models;

public record SFTPAttributes(
    ulong FileSize,
    uint Uid,
    uint Gid,
    Permissions Permissions,
    DateTimeOffset LastAccessedTime,
    DateTimeOffset LastModifiedTime
)
{
    private static readonly uint _defaultowner = 0;    // 65534 should be "nobody"
    private static readonly uint _defaultgroup = _defaultowner;

    private static readonly Permissions _defaultpermissions =
        Permissions.User_Execute
        | Permissions.User_Read
        | Permissions.User_Write
        | Permissions.Group_Read;


    public IDictionary<string, string> ExtendeAttributes { get; } = new Dictionary<string, string>();
    public string GetLongFileName(string name)
        => ((FormattableString)$"{GetPermissionBits()} {1,3} {LookupId(Uid),-8} {LookupId(Gid),-8} {FileSize,8} {LastModifiedTime,12:MMM dd HH:mm} {name}").ToString(CultureInfo.InvariantCulture);

    private static string LookupId(uint id) => id switch
    {
        0 => "root",
        65534 => "nobody",
        _ => "unknown"
    };

    private string GetPermissionBits()
        => $"{(Permissions.HasFlag(Permissions.Directory) ? "d" : "-")}{AttrStr(Permissions.HasFlag(Permissions.User_Read), Permissions.HasFlag(Permissions.User_Write), Permissions.HasFlag(Permissions.User_Execute))}{AttrStr(Permissions.HasFlag(Permissions.Group_Read), Permissions.HasFlag(Permissions.Group_Write), Permissions.HasFlag(Permissions.Group_Execute))}{AttrStr(Permissions.HasFlag(Permissions.Other_Read), Permissions.HasFlag(Permissions.Other_Write), Permissions.HasFlag(Permissions.Other_Execute))}";

    private static string AttrStr(bool read, bool write, bool execute)
        => $"{(read ? "r" : "-")}{(write ? "w" : "-")}{(execute ? "x" : "-")}";

    public SFTPAttributes(FileSystemInfo fsInfo)
        : this(
              GetLength(fsInfo),
              _defaultowner,
              _defaultgroup,
              _defaultpermissions | GetFileTypeBits(fsInfo),
              fsInfo.LastAccessTimeUtc,
              fsInfo.LastWriteTimeUtc
            )
    { }

    private static Permissions GetFileTypeBits(FileSystemInfo fsInfo)
        => fsInfo switch
        {
            DirectoryInfo => Permissions.Directory,
            FileInfo => Permissions.Regular_File,
            _ => Permissions.None
        };

    private static ulong GetLength(FileSystemInfo fsInfo)
        => fsInfo switch
        {
            FileInfo => (ulong)((FileInfo)fsInfo).Length,
            _ => 0
        };
}
