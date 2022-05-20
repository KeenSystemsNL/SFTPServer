namespace SFTP.Enums;

[Flags]
public enum Permissions : uint
{
    None = 0,
    OtherExecute = 0x01,
    OtherWrite = 0x02,
    OtherRead = 0x04,
    GroupExecute = 0x08,
    GroupWrite = 0x10,
    GroupRead = 0x20,
    UserExecute = 0x40,
    UserWrite = 0x80,
    UserRead = 0x100,
    Sticky = 0x200,
    SetGID = 0x400,
    SetUID = 0x800,
    //FIFO = 0x1000,
    //CharacterDevice = 0x2000,
    Directory = 0x4000,
    //Block_Device = 0x6000,
    RegularFile = 0x8000,
    //SymbolicLink = 0xA000,
    //Socket = 0x000
}