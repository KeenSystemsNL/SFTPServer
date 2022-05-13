namespace SFTPTest.Enums;

public enum Status : uint
{
    OK = 0x00,
    EOF = 0x01,
    NO_SUCH_FILE = 0x02,
    PERMISSION_DENIED = 0x03,
    FAILURE = 0x04,
    BAD_MESSAGE = 0x05,
    NO_CONNECTION = 0x06,
    CONNECTION_LOST = 0x07,
    OP_UNSUPPORTED = 0x08,
    INVALID_HANDLE = 0x09,
    NO_SUCH_PATH = 0x0A,
    FILE_ALREADY_EXISTS = 0x0B,
    WRITE_PROTECT = 0x0C,
    NO_MEDIA = 0x0D
}