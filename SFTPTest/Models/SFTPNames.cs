using SFTPTest.Enums;

namespace SFTPTest.Models;

public record SFTPNames(IEnumerable<SFTPName> Names)
{
    public Status Status { get; init; } = Status.OK;
    public static readonly SFTPNames EOF = new(Array.Empty<SFTPName>()) { Status = Status.EOF };
}
