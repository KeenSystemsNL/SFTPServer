using SFTPTest.Enums;

namespace SFTPTest.Models;

public record SFTPData(byte[] Data)
{
    public Status Status { get; init; } = Status.OK;
    public static readonly SFTPData EOF = new(Array.Empty<byte>()) { Status = Status.EOF };
}