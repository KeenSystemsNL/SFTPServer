using System.Buffers.Binary;
using System.Text;

namespace SFTPTest;

internal static class Dumper
{
    public static string Dump(uint data)
        => Dump(BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(data)));

    public static string Dump(string data)
        => Dump(Encoding.UTF8.GetBytes(data));

    public static string Dump(byte[] data)
        => string.Join(" ", data.Select(b => b.ToString("X2")));

    public static string DumpASCII(byte[] data)
        => string.Join(" ", data.Select(b => (b >= 32 && b < 127 ? (char)b : '.').ToString().PadLeft(2)));

}