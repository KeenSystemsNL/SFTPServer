namespace SFTPTest.Enums;

[Flags]
public enum Permissions : uint
{
    None = 0,
    Other_Execute = 0b000000001,
    Other_Write = 0b000000010,
    Other_Read = 0b000000100,
    Group_Execute = 0b000001000,
    Group_Write = 0b000010000,
    Group_Read = 0b000100000,
    Owner_Execute = 0b001000000,
    Owner_Write = 0b010000000,
    Owner_Read = 0b100000000,
    Sticky = 0b1000000000,
    SetGID = 0b10000000000,
    SetUID = 0b100000000000
}
