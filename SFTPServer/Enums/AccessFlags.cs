namespace SFTP.Enums;

[Flags]
internal enum AccessFlags : uint
{
    Read = 0x01,
    Write = 0x02,
    Append = 0x04,
    Create = 0x08,
    Truncate = 0x10,
    Exclusive = 0x20,
    Text = 0x40
}

internal static class AccessFlagsExtensionMethods
{
    public static FileMode ToFileMode(this AccessFlags flags)
    {
        var filemode = FileMode.Open;
        if (flags.HasFlag(AccessFlags.Append))
        {
            filemode = FileMode.Append;
        }
        else if (flags.HasFlag(AccessFlags.Create))
        {
            filemode = FileMode.OpenOrCreate;
        }
        else if (flags.HasFlag(AccessFlags.Truncate))
        {
            filemode = FileMode.CreateNew;
        }
        else if (flags.HasFlag(AccessFlags.Exclusive))
        {
            throw new NotImplementedException();
        }
        else if (flags.HasFlag(AccessFlags.Text))
        {
            throw new NotImplementedException();
        }
        return filemode;
    }

    public static FileAccess ToFileAccess(this AccessFlags flags)
    {
        var fileAccess = FileAccess.Read;
        if (flags.HasFlag(AccessFlags.Read) && flags.HasFlag(AccessFlags.Write))
        {
            fileAccess = FileAccess.ReadWrite;
        }
        else if (flags.HasFlag(AccessFlags.Read))
        {
            fileAccess = FileAccess.Read;
        }
        else if (flags.HasFlag(AccessFlags.Write))
        {
            fileAccess = FileAccess.Write;
        }
        else if (flags.HasFlag(AccessFlags.Text))
        {
            throw new NotImplementedException();
        }
        return fileAccess;
    }
}