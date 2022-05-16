using SFTPTest.Enums;

namespace SFTPTest.Infrastructure;

public record Attributes(
    FileType FileType,
    ulong FileSize,
    string Uid,
    string Gid,
    Permissions Permissions,
    DateTimeOffset CreationTime,
    DateTimeOffset LastAccessedTime,
    DateTimeOffset LastModifiedTime,
    ACL[] ACLs)
{
    private static readonly string _owner = "Nobody";
    private static readonly string _group = _owner;

    private static readonly Permissions _defaultpermissions = Permissions.Owner_Execute
        | Permissions.Owner_Read
        | Permissions.Owner_Write
        | Permissions.Group_Write
        | Permissions.Other_Execute;

    public static Attributes Dummy = new(FileType.UNKNOWN, 0, _owner, _group, _defaultpermissions, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, Array.Empty<ACL>());
    public Attributes(FileSystemInfo fsInfo) : this(GetFileType(fsInfo), GetLength(fsInfo), _owner, _group, _defaultpermissions, fsInfo.CreationTimeUtc, fsInfo.LastAccessTimeUtc, fsInfo.LastWriteTimeUtc, Array.Empty<ACL>()) { }

    private static FileType GetFileType(FileSystemInfo fsInfo)
        => fsInfo switch
        {
            FileInfo => FileType.REGULAR,
            DirectoryInfo => FileType.DIRECTORY,
            _ => FileType.UNKNOWN,
        };

    private static ulong GetLength(FileSystemInfo fsInfo)
        => fsInfo switch
        {
            FileInfo => (ulong)((FileInfo)fsInfo).Length,
            _ => 0
        };
}
