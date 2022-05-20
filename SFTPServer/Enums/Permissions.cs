namespace SFTP.Enums;

[Flags]
public enum Permissions : uint
{
    None = 0,
    Other_Execute = 0x01,
    Other_Write = 0x02,
    Other_Read = 0x04,
    Group_Execute = 0x08,
    Group_Write = 0x10,
    Group_Read = 0x20,
    User_Execute = 0x40,
    User_Write = 0x80,
    User_Read = 0x100,
    Sticky = 0x200,
    SetGID = 0x400,
    SetUID = 0x800,
    //FIFO = 0x1000,
    //Character_Device = 0x2000,
    Directory = 0x4000,
    //Block_Device = 0x6000,
    Regular_File = 0x8000,
    //Symbolic_Link = 0xA000,
    //Socket = 0x000
}