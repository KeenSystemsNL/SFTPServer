namespace SFTP.Enums;

[Flags]
internal enum PFlags : uint
{
    Size = 0x00000001,
    UidGid = 0x00000002,
    Permissions = 0x00000004,
    AccessModifiedTime = 0x00000008,
    Extended = 0x80000000,

    DEFAULT = Size
           | UidGid
           | Permissions
           | AccessModifiedTime
}
