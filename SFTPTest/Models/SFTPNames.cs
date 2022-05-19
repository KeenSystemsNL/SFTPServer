using SFTPTest.Enums;

namespace SFTPTest.Models;

public record SFTPNames(IEnumerable<FileSystemInfo> Names)
{
    public Status Status { get; init; } = Status.OK;
    public static readonly SFTPNames EOF = new(Array.Empty<FileSystemInfo>()) { Status = Status.EOF };
}
