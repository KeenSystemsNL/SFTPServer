using Microsoft.Extensions.Logging;
using System.Text;

namespace SFTPTest;

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

    public void Write(MessageType messageType)
    => _memorystream.WriteByte((byte)messageType);

    public void Write(bool value)
        => _memorystream.WriteByte(value ? (byte)1 : (byte)0);

    public void Write(byte value)
        => _memorystream.WriteByte(value);

    public void Write(uint value)
    {
        var bytes = new[] { (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)(value & 0xFF) };
        _memorystream.Write(bytes, 0, 4);
    }

    public void Write(ulong value)
    {
        var bytes = new[] {
                (byte)(value >> 56), (byte)(value >> 48), (byte)(value >> 40), (byte)(value >> 32),
                (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)(value & 0xFF)
            };
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

        var len = new[] { (byte)(data.Length >> 24), (byte)(data.Length >> 16), (byte)(data.Length >> 8), (byte)(data.Length & 0xFF) };

        var packet = len.Concat(data).ToArray();

        _stream.Write(packet, 0, packet.Length);
        _memorystream.Position = 0;
        _memorystream.SetLength(0);
    }
}