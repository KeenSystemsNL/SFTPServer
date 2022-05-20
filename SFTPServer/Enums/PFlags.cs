namespace SFTP.Enums;

[Flags]
internal enum PFlags : uint
{
    SIZE = 0x00000001,
    UIDGUID = 0x00000002,
    PERMISSIONS = 0x00000004,
    ACMODTIME = 0x00000008,
    EXTENDED = 0x80000000,

    DEFAULT = SIZE
           | UIDGUID
           | PERMISSIONS
           | ACMODTIME
}
