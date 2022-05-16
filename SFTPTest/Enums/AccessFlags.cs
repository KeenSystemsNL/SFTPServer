namespace SFTPTest.Enums;

[Flags]
public enum AccessFlags : uint
{
    READ = 0x01,
    WRITE = 0x02,
    APPEND = 0x04,
    CREATE = 0x08,
    TRUNCATE = 0x10,
    EXCL = 0x20,
    TEXT = 0x40
}

public static class AccessFlagsExtensionMethods
{
    public static FileMode ToFileMode(this AccessFlags flags)
    {
        var filemode = FileMode.Open;
        if (flags.HasFlag(AccessFlags.APPEND))
        {
            filemode = FileMode.Append;
        }
        else if (flags.HasFlag(AccessFlags.CREATE))
        {
            filemode = FileMode.OpenOrCreate;
        }
        else if (flags.HasFlag(AccessFlags.TRUNCATE))
        {
            filemode = FileMode.CreateNew;
        }
        else if (flags.HasFlag(AccessFlags.EXCL))
        {
            throw new NotImplementedException();
        }
        else if (flags.HasFlag(AccessFlags.TEXT))
        {
            throw new NotImplementedException();
        }
        return filemode;
    }

    public static FileAccess ToFileAccess(this AccessFlags flags)
    {
        var fileAccess = FileAccess.Read;
        if (flags.HasFlag(AccessFlags.READ) && flags.HasFlag(AccessFlags.WRITE))
        {
            fileAccess = FileAccess.ReadWrite;
        }
        else if (flags.HasFlag(AccessFlags.READ))
        {
            fileAccess = FileAccess.Read;
        }
        else if (flags.HasFlag(AccessFlags.WRITE))
        {
            fileAccess = FileAccess.Write;
        }
        else if (flags.HasFlag(AccessFlags.TEXT))
        {
            throw new NotImplementedException();
        }
        return fileAccess;
    }
}