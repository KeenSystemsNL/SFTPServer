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

    private byte[] ReadBinary()
        => ReadBinary((int)ReadUInt32());


    private byte[] ReadBinary(int length)
    {
        var data = new byte[length];
        _stream.Read(data, 0, length);

        return data;
    }
}
