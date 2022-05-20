using SFTP.Enums;

namespace SFTP.Models;

public record SFTPData(byte[] Data)
{
    public Status Status { get; init; } = Status.OK;
    public static readonly SFTPData EOF = new(Array.Empty<byte>()) { Status = Status.EOF };
}