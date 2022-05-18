namespace SFTPTest.Enums;

public enum RequestType : byte
{
    INIT = 0x01,
    VERSION = 0x02,
    OPEN = 0x03,
    CLOSE = 0x04,
    READ = 0x05,
    WRITE = 0x06,
    LSTAT = 0x07,
    FSTAT = 0x08,
    SETSTAT = 0x09,
    FSETSTAT = 0x0A,
    OPENDIR = 0x0B,
    READDIR = 0x0C,
    REMOVE = 0x0D,
    MKDIR = 0x0E,
    RMDIR = 0x0F,
    REALPATH = 0x10,
    STAT = 0x11,
    RENAME = 0x12,
    READLINK = 0x13,
    SYMLINK = 0x14,

    EXTENDED = 0xC8
}
