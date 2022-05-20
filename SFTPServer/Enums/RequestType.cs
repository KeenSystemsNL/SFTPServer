namespace SFTP.Enums;

internal enum RequestType : byte
{
    Init = 0x01,
    Version = 0x02,
    Open = 0x03,
    Close = 0x04,
    Read = 0x05,
    Write = 0x06,
    LStat = 0x07,
    FStat = 0x08,
    SetStat = 0x09,
    FSetStat = 0x0A,
    OpenDir = 0x0B,
    ReadDir = 0x0C,
    Remove = 0x0D,
    MakeDir = 0x0E,
    RemoveDir = 0x0F,
    RealPath = 0x10,
    Stat = 0x11,
    Rename = 0x12,
    ReadLink = 0x13,
    SymLink = 0x14,

    Extended = 0xC8
}
