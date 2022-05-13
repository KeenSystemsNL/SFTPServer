namespace SFTPTest;

public enum FileType
{
    REGULAR = 1,
    DIRECTORY = 2,
    SYMLINK = 3,
    SPECIAL = 4,
    UNKNOWN = 5,
    SOCKET = 6,
    CHAR_DEVICE = 7,
    BLOCK_DEVICE = 8,
    FIFO = 9
}