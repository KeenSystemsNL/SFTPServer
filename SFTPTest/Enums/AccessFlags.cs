namespace SFTPTest.Enums;

[Flags]
public enum AccessFlags : uint
{
    READ = 0x01,
    WRITE = 0x02,
    APPEND = 0x04,
    CREATE = 0x08,
    TRUNCATE = 0x10,
    EXCL = 0x20,
    TEXT = 0x40
}