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
    {
        var data = ReadBinary(4);
        return (uint)(data[0] << 24 | data[1] << 16 | data[2] << 8 | data[3]);
    }

    public ulong ReadUInt64()
    {
        var data = ReadBinary(8);
        return (ulong)data[0] << 56 | (ulong)data[1] << 48 | (ulong)data[2] << 40 | (ulong)data[3] << 32 |
                (ulong)data[4] << 24 | (ulong)data[5] << 16 | (ulong)data[6] << 8 | data[7];
    }

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
