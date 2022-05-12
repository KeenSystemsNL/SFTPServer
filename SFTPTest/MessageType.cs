namespace SFTPTest;

// https://datatracker.ietf.org/doc/html/draft-ietf-secsh-filexfer-13#section-4.3
public enum MessageType : byte
{
    INIT = 1,
    VERSION = 2,
    OPEN = 3,
    CLOSE = 4,
    READ = 5,
    WRITE = 6,
    LSTAT = 7,
    FSTAT = 8,
    SETSTAT = 9,
    FSETSTAT = 10,
    OPENDIR = 11,
    READDIR = 12,
    REMOVE = 13,
    MKDIR = 14,
    RMDIR = 15,
    REALPATH = 16,
    STAT = 17,
    RENAME = 18,
    READLINK = 19,
    LINK = 21,
    BLOCK = 22,
    UNBLOCK = 23,
    STATUS = 101,
    HANDLE = 102,
    DATA = 103,
    NAME = 104,
    ATTRS = 105,
    EXTENDED = 200,
    EXTENDED_REPLY = 201
}
