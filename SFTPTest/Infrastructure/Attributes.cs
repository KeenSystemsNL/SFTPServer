using SFTPTest.Enums;

namespace SFTPTest.Infrastructure;

internal record Attributes(FileType FileType, ulong FileSize, string Uid, string Gid, Permissions Permissions, DateTimeOffset CTime, DateTimeOffset ATime, DateTimeOffset MTime)
{
    private static readonly string _owner = "Nobody";
    private static readonly string _group = _owner;

    private static readonly Permissions _defaultpermissions = Permissions.Owner_Execute
        | Permissions.Owner_Read
        | Permissions.Owner_Write
        | Permissions.Group_Read
        | Permissions.Other_Read;

    public static Attributes Dummy = new(FileType.UNKNOWN, 0, _owner, _group, _defaultpermissions, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch);
    public Attributes(FileSystemInfo fsInfo) : this(GetFileType(fsInfo), GetLength(fsInfo), _owner, _group, _defaultpermissions, fsInfo.CreationTimeUtc, fsInfo.LastAccessTimeUtc, fsInfo.LastWriteTimeUtc) { }

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
