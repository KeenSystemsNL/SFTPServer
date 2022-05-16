namespace SFTPTest.Enums;

public enum FileType : byte
{
    REGULAR = 1,
    DIRECTORY = 2,
    SYMLINK = 3,
    SPECIAL = 4,
    UNKNOWN = 5
}
