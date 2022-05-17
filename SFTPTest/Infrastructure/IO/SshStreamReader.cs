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

    public async Task<bool> ReadBoolean(CancellationToken cancellationToken = default)
        => await ReadByte(cancellationToken).ConfigureAwait(false) != 0;

    public async Task<byte> ReadByte(CancellationToken cancellationToken = default)
        => (await ReadBinary(1, cancellationToken).ConfigureAwait(false))[0];

    public async Task<uint> ReadUInt32(CancellationToken cancellationToken = default)
        => BinaryPrimitives.ReadUInt32BigEndian(await ReadBinary(4, cancellationToken).ConfigureAwait(false));

    public async Task<ulong> ReadUInt64(CancellationToken cancellationToken = default)
        => BinaryPrimitives.ReadUInt64BigEndian(await ReadBinary(8, cancellationToken).ConfigureAwait(false));

    public async Task<long> ReadInt64(CancellationToken cancellationToken = default)
        => BinaryPrimitives.ReadInt64BigEndian(await ReadBinary(8, cancellationToken).ConfigureAwait(false));

    public async Task<string> ReadString(int length, CancellationToken cancellationToken = default)
        => _encoding.GetString(await ReadBinary(length, cancellationToken).ConfigureAwait(false));

    public async Task<string> ReadString(CancellationToken cancellationToken = default)
        => _encoding.GetString(await ReadBinary(cancellationToken).ConfigureAwait(false));

    public async Task<AccessFlags> ReadAccessFlags(CancellationToken cancellationToken = default)
        => (AccessFlags)await ReadUInt32(cancellationToken).ConfigureAwait(false);

    public async Task<FileAttributeFlags> ReadFileAttributeFlags(CancellationToken cancellationToken = default)
        => (FileAttributeFlags)await ReadUInt32(cancellationToken).ConfigureAwait(false);

    public async Task<DateTimeOffset> ReadTime(bool subseconds, CancellationToken cancellationToken = default)
    {
        var seconds = await ReadInt64(cancellationToken).ConfigureAwait(false);
        return seconds > 0
            ? DateTimeOffset.FromUnixTimeSeconds(seconds)
                .AddMilliseconds((subseconds ? await ReadUInt32(cancellationToken).ConfigureAwait(false) : 0) * 10 ^ 6)
            : DateTimeOffset.MinValue;
    }

    public async Task<Attributes> ReadAttributes(CancellationToken cancellationToken = default)
    {
        var flags = (FileAttributeFlags)await ReadUInt32(cancellationToken).ConfigureAwait(false);
        var type = (FileType)await ReadByte(cancellationToken).ConfigureAwait(false);
        var size = flags.HasFlag(FileAttributeFlags.SIZE) ? await ReadUInt64(cancellationToken).ConfigureAwait(false) : 0;
        var owner = flags.HasFlag(FileAttributeFlags.OWNERGROUP) ? await ReadString(cancellationToken).ConfigureAwait(false) : string.Empty;
        var group = flags.HasFlag(FileAttributeFlags.OWNERGROUP) ? await ReadString(cancellationToken).ConfigureAwait(false) : string.Empty;
        var permissions = flags.HasFlag(FileAttributeFlags.PERMISSIONS) ? (Permissions)await ReadUInt32(cancellationToken).ConfigureAwait(false) : Permissions.None;
        var atime = flags.HasFlag(FileAttributeFlags.ACCESSTIME) ? await ReadTime(flags.HasFlag(FileAttributeFlags.SUBSECOND_TIMES), cancellationToken).ConfigureAwait(false) : DateTimeOffset.MinValue;
        var ctime = flags.HasFlag(FileAttributeFlags.CREATETIME) ? await ReadTime(flags.HasFlag(FileAttributeFlags.SUBSECOND_TIMES), cancellationToken).ConfigureAwait(false) : DateTimeOffset.MinValue;
        var mtime = flags.HasFlag(FileAttributeFlags.MODIFYTIME) ? await ReadTime(flags.HasFlag(FileAttributeFlags.SUBSECOND_TIMES), cancellationToken).ConfigureAwait(false) : DateTimeOffset.MinValue;
        var acls = flags.HasFlag(FileAttributeFlags.ACL) ? await ReadACLs(cancellationToken).ConfigureAwait(false) : Array.Empty<ACL>();
        var extended_count = flags.HasFlag(FileAttributeFlags.EXTENDED) ? await ReadUInt32(cancellationToken).ConfigureAwait(false) : 0;

        if (extended_count > 0)
        {
            throw new Exception("Extended attributes currently not supported");
        }

        return new Attributes(type, size, owner, group, permissions, ctime, atime, mtime, acls);
    }

    public async Task<ACL[]> ReadACLs(CancellationToken cancellationToken = default)
    {
        var acecount = await ReadUInt32(cancellationToken).ConfigureAwait(false);
        var acls = new ACL[(int)acecount];
        for (var i = 0; i < acecount; i++)
        {
            acls[i] = new ACL(
                (ACEType)await ReadUInt32(cancellationToken).ConfigureAwait(false),
                (ACEFlags)await ReadUInt32(cancellationToken).ConfigureAwait(false),
                (ACEMask)await ReadUInt32(cancellationToken).ConfigureAwait(false),
                await ReadString(cancellationToken).ConfigureAwait(false)
            );
        }
        return acls;
    }

    public async Task<byte[]> ReadBinary(CancellationToken cancellationToken = default)
        => await ReadBinary((int)await ReadUInt32(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);


    private async Task<byte[]> ReadBinary(int length, CancellationToken cancellationToken = default)
    {
        var buffer = new byte[length];
        await _stream.ReadAsync(buffer.AsMemory(0, length), cancellationToken).ConfigureAwait(false);

        return buffer;
    }
}
