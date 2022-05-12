namespace SFTPTest.Infrastructure;

internal record Attributes(uint Flags, ulong FileSize, uint Uid, uint Gid, uint Permissions, DateTimeOffset ATime, DateTimeOffset MTime)
{
    public Attributes(long FileSize) : this(uint.MaxValue, (ulong)FileSize, uint.MaxValue, uint.MaxValue, uint.MaxValue, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow) { }
    public Attributes(FileInfo fileInfo) : this(fileInfo.Length) { }
}
