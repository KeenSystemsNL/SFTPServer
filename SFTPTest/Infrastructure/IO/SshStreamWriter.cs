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

    public void Write(FileType fileType)
        => Write((byte)fileType);

    public void Write(Status status)
        => Write((uint)status);


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
        if (str == null)
        {
            throw new ArgumentNullException(nameof(str));
        }
        WriteBinary(_encoding.GetBytes(str));
    }

    public void Write(byte[] data)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        _memorystream.Write(data, 0, data.Length);
    }

    public void WriteBinary(byte[] buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException(nameof(buffer));
        }

        Write((uint)buffer.Length);
        _memorystream.Write(buffer, 0, buffer.Length);
    }

    public void WriteBinary(byte[] buffer, int offset, int count)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException(nameof(buffer));
        }

        Write((uint)count);
        _memorystream.Write(buffer, offset, count);
    }

    public void Flush(ILogger logger)
    {
        var data = _memorystream.ToArray();

        logger.LogInformation("Writing: {data}", Dumper.Dump(data));
        logger.LogInformation("Writing: {data}", Dumper.DumpASCII(data));

        var len = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(len, (uint)data.Length);

        var packet = len.Concat(data).ToArray();

        _stream.Write(packet, 0, packet.Length);
        _memorystream.Position = 0;
        _memorystream.SetLength(0);
    }
}