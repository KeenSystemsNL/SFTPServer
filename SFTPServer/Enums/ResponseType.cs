namespace SFTPTest.Enums;

internal enum ResponseType : byte
{
    STATUS = 0x65,
    HANDLE = 0x66,
    DATA = 0x67,
    NAME = 0x68,
    ATTRS = 0x69,

    EXTENDED_REPLY = 0xC9
}