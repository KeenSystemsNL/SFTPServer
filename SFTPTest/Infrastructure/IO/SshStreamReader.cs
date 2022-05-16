using SFTPTest.Enums;
using System.Buffers.Binary;
using System.Text;

namespace SFTPTest.Infrastructure.IO;

public class SshStreamReader
{
    private readonly Stream _stream;
    private static readonly Encoding _encoding = new UTF8Encoding(false);

    public SshStreamReader(Stream stream)
        => _stream = stream ?? throw new ArgumentNullException(nameof(stream));

    public bool ReadBoolean()
        => ReadByte() != 0;

    public byte ReadByte()
        => ReadBinary(1)[0];

    public uint ReadUInt32()
        => BinaryPrimitives.ReadUInt32BigEndian(ReadBinary(4));

    public ulong ReadUInt64()
        => BinaryPrimitives.ReadUInt64BigEndian(ReadBinary(8));
    public long ReadInt64()
        => BinaryPrimitives.ReadInt64BigEndian(ReadBinary(8));

    public string ReadString(int length)
        => _encoding.GetString(ReadBinary(length));

    public string ReadString()
        => _encoding.GetString(ReadBinary());

    public AccessFlags ReadAccessFlags()
        => (AccessFlags)ReadUInt32();

    public FileAttributeFlags ReadFileAttributeFlags()
        => (FileAttributeFlags)ReadUInt32();

    public DateTimeOffset ReadTime(bool subseconds)
        => DateTimeOffset.FromUnixTimeSeconds(ReadInt64())
            .AddMilliseconds((subseconds ? ReadUInt32() : 0) * 10 ^ 6);

    public Attributes ReadAttributes()
    {
        var flags = (FileAttributeFlags)ReadUInt32();
        var type = (FileType)ReadByte();
        var size = flags.HasFlag(FileAttributeFlags.SIZE) ? ReadUInt64() : 0;
        var owner = flags.HasFlag(FileAttributeFlags.OWNERGROUP) ? ReadString() : string.Empty;
        var group = flags.HasFlag(FileAttributeFlags.OWNERGROUP) ? ReadString() : string.Empty;
        var permissions = flags.HasFlag(FileAttributeFlags.PERMISSIONS) ? (Permissions)ReadUInt32() : Permissions.None;
        var atime = flags.HasFlag(FileAttributeFlags.ACCESSTIME) ? ReadTime(flags.HasFlag(FileAttributeFlags.SUBSECOND_TIMES)) : DateTime.MinValue;
        var ctime = flags.HasFlag(FileAttributeFlags.CREATETIME) ? ReadTime(flags.HasFlag(FileAttributeFlags.SUBSECOND_TIMES)) : DateTime.MinValue;
        var mtime = flags.HasFlag(FileAttributeFlags.MODIFYTIME) ? ReadTime(flags.HasFlag(FileAttributeFlags.SUBSECOND_TIMES)) : DateTime.MinValue;
        var acls = flags.HasFlag(FileAttributeFlags.ACL) ? ReadACLs() : Array.Empty<ACL>();
        var extended_count = flags.HasFlag(FileAttributeFlags.EXTENDED) ? ReadUInt32() : 0;

        if (extended_count > 0)
        {
            throw new Exception("Extended attributes currently not supported");
        }

        return new Attributes(type, size, owner, group, permissions, ctime, atime, mtime, acls);
    }

    public ACL[] ReadACLs()
    {
        var acecount = ReadUInt32();
        var acls = new ACL[(int)acecount];
        for (var i = 0; i < acecount; i++)
        {
            acls[i] = new ACL(
                (ACEType)ReadUInt32(),
                (ACEFlags)ReadUInt32(),
                (ACEMask)ReadUInt32(),
                ReadString()
            );
        }
        return acls;
    }

    public byte[] ReadBinary()
        => ReadBinary((int)ReadUInt32());


    private byte[] ReadBinary(int length)
    {
        var data = new byte[length];
        _stream.Read(data, 0, length);

        return data;
    }
}
