namespace SFTPTest.Enums;

[Flags]
public enum FileAttributeFlags : uint
{
    SIZE = 0x00000001,
    PERMISSIONS = 0x00000004,
    ACCESSTIME = 0x00000008,
    CREATETIME = 0x00000010,
    MODIFYTIME = 0x00000020,
    ACL = 0x00000040,
    OWNERGROUP = 0x00000080,
    SUBSECOND_TIMES = 0x00000100,
    EXTENDED = 0x80000000,

    DEFAULT = SIZE
           | OWNERGROUP
           | PERMISSIONS
           | ACCESSTIME
           | CREATETIME
           | MODIFYTIME
           | ACL
}
