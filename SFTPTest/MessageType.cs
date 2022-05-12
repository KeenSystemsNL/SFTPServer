namespace SFTPTest;

// https://datatracker.ietf.org/doc/html/draft-ietf-secsh-filexfer-13#section-4.3
public enum MessageType : byte
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
    LINK = 0x15,
    BLOCK = 0x16,
    UNBLOCK = 0x17,
    STATUS = 0x65,
    HANDLE = 0x66,
    DATA = 0x67,
    NAME = 0x68,
    ATTRS = 0x69,
    EXTENDED = 0xC8,
    EXTENDED_REPLY = 0xC9
}
