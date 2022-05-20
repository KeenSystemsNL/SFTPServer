namespace SFTP.Enums;

internal enum ResponseType : byte
{
    Status = 0x65,
    Handle = 0x66,
    Data = 0x67,
    Name = 0x68,
    Attributes = 0x69,

    Extended = 0xC9
}