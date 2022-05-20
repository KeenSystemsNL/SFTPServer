using SFTP.Enums;

namespace SFTP.Models;

public record SFTPData(byte[] Data)
{
    public Status Status { get; init; } = Status.Ok;
    public static readonly SFTPData EOF = new(Array.Empty<byte>()) { Status = Status.EndOfFile };
}