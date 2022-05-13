namespace SFTPTest.Infrastructure;

internal record Attributes(FileType FileType, ulong FileSize, string Uid, string Gid, DateTimeOffset CTime, DateTimeOffset ATime, DateTimeOffset MTime)
{
    private static readonly string _nobody = "Nobody";
    public static Attributes Dummy = new Attributes(FileType.UNKNOWN, 0, _nobody, _nobody, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch);
    public Attributes(FileSystemInfo fsInfo) : this(GetFileType(fsInfo), GetLength(fsInfo), _nobody, _nobody, fsInfo.CreationTimeUtc, fsInfo.LastAccessTimeUtc, fsInfo.LastWriteTimeUtc) { }

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
