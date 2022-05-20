namespace SFTP.Enums;

public enum Status : uint
{
    Ok = 0x00,
    EndOfFile = 0x01,
    NoSuchFile = 0x02,
    PermissionDenied = 0x03,
    Failure = 0x04,
    BadMessage = 0x05,
    NoConnection = 0x06,
    ConnectionLost = 0x07,
    OperationUnsupported = 0x08
}