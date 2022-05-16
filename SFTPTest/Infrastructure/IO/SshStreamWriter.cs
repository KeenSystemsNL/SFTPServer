using Microsoft.Extensions.Logging;
using SFTPTest.Enums;
using System.Buffers.Binary;
using System.Text;

namespace SFTPTest.Infrastructure.IO;

public class SshStreamWriter
{
    private readonly Stream _stream;
    private readonly MemoryStream _memorystream;
    private static readonly Encoding _encoding = new UTF8Encoding(false);

    public SshStreamWriter(Stream stream, int bufferSize)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _memorystream = new MemoryStream(bufferSize);
    }

    public void Write(RequestType requestType)
        => Write((byte)requestType);

    public void Write(ResponseType responseType)
        => Write((byte)responseType);

    public void Write(FileAttributeFlags fileAttributeFlags)
        => Write((uint)fileAttributeFlags);

    public void Write(Permissions permissions)
        => Write((uint)permissions);

    public void Write(ACEType aceType)
        => Write((uint)aceType);

    public void Write(ACEFlags aceFlags)
    => Write((uint)aceFlags);

    public void Write(ACEMask aceMask)
    => Write((uint)aceMask);

    public void Write(FileType fileType)
        => Write((byte)fileType);

    public void Write(Status status)
        => Write((uint)status);

    public void Write(Attributes attributes, FileAttributeFlags flags = FileAttributeFlags.DEFAULT)
    {
        Write(flags);
        Write(attributes.FileType);
        if (flags.HasFlag(FileAttributeFlags.SIZE))
        {
            Write(attributes.FileSize);
        }

        if (flags.HasFlag(FileAttributeFlags.OWNERGROUP))
        {
            Write(attributes.Uid);
            Write(attributes.Gid);
        }
        if (flags.HasFlag(FileAttributeFlags.PERMISSIONS))
        {
            Write(attributes.Permissions);
        }

        if (flags.HasFlag(FileAttributeFlags.ACCESSTIME))
        {
            Write(attributes.LastAccessedTime, flags.HasFlag(FileAttributeFlags.SUBSECOND_TIMES));
        }

        if (flags.HasFlag(FileAttributeFlags.CREATETIME))
        {
            Write(attributes.CreationTime, flags.HasFlag(FileAttributeFlags.SUBSECOND_TIMES));
        }

        if (flags.HasFlag(FileAttributeFlags.MODIFYTIME))
        {
            Write(attributes.LastModifiedTime, flags.HasFlag(FileAttributeFlags.SUBSECOND_TIMES));
        }

        if (flags.HasFlag(FileAttributeFlags.ACL))
        {
            Write(attributes.ACLs);
        }
        //Write(0);  //extended type
        //Write(0);  //extended data
    }

    public void Write(DateTimeOffset dateTime, bool subseconds)
    {
        Write(dateTime.ToUnixTimeSeconds());
        if (subseconds)
        {
            Write(dateTime.Millisecond * 10 ^ 6);
        }
    }

    public void Write(ACL[] acls)
    {
        Write(acls.Length);
        foreach (var acl in acls)
        {
            Write(acl.ACEType);
            Write(acl.ACEFlags);
            Write(acl.ACEMask);
            Write(acl.Who);
        }
    }

    public void Write(bool value)
        => _memorystream.WriteByte(value ? (byte)1 : (byte)0);

    public void Write(byte value)
        => _memorystream.WriteByte(value);

    public void Write(int value)
        => Write((uint)value);

    public void Write(long value)
        => Write((ulong)value);

    public void Write(uint value)
    {
        var bytes = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
        _memorystream.Write(bytes, 0, 4);
    }

    public void Write(ulong value)
    {
        var bytes = new byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(bytes, value);
        _memorystream.Write(bytes, 0, 8);
    }

    public void Write(string str)
    {
        if (str is null)
        {
            throw new ArgumentNullException(nameof(str));
        }
        Write((uint)str.Length);
        Write(_encoding.GetBytes(str));
    }

    public void Write(ReadOnlySpan<byte> data)
        => _memorystream.Write(data);

    public void Flush(ILogger logger)
    {
        var data = _memorystream.ToArray();

        logger.LogInformation("Writing [{length}]: {data}", data.Length, Dumper.Dump(data));
        logger.LogInformation("Writing [{length}]: {data}", data.Length, Dumper.DumpASCII(data));

        var len = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(len, (uint)data.Length);

        _stream.Write(len, 0, len.Length);
        _stream.Write(data, 0, data.Length);

        _memorystream.Position = 0;
        _memorystream.SetLength(0);
    }
}