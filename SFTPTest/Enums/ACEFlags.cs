namespace SFTPTest.Enums;

[Flags]
public enum ACEFlags : uint
{
    FILE_INHERIT = 0x01,
    DIRECTORY_INHERIT = 0x02,
    NO_PROPAGATE_INHERIT = 0x04,
    INHERIT_ONLY = 0x08,
    SUCCESSFUL_ACCES = 0x10,
    FAILED_ACCESS = 0x20,
    IDENTIFIER_GROUP = 0x40,
}
