using SFTP.Enums;

namespace SFTP.Models;

public record SFTPNames(IEnumerable<SFTPName> Names)
{
    public Status Status { get; init; } = Status.Ok;
    public static readonly SFTPNames EOF = new(Array.Empty<SFTPName>()) { Status = Status.EndOfFile };
}
