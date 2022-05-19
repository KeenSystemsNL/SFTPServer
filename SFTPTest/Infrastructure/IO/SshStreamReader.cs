﻿using SFTPTest.Enums;
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

    public async Task<DateTimeOffset> ReadTime(CancellationToken cancellationToken = default)
    {
        var seconds = await ReadUInt32(cancellationToken).ConfigureAwait(false);
        return seconds > 0
            ? DateTimeOffset.FromUnixTimeSeconds(seconds)
            : DateTimeOffset.MinValue;
    }

    public async Task<Attributes> ReadAttributes(CancellationToken cancellationToken = default)
    {
        var flags = (FileAttributeFlags)await ReadUInt32(cancellationToken).ConfigureAwait(false);
        var size = flags.HasFlag(FileAttributeFlags.SIZE) ? await ReadUInt64(cancellationToken).ConfigureAwait(false) : 0;
        var owner = flags.HasFlag(FileAttributeFlags.UIDGUID) ? await ReadUInt32(cancellationToken).ConfigureAwait(false) : 0;
        var group = flags.HasFlag(FileAttributeFlags.UIDGUID) ? await ReadUInt32(cancellationToken).ConfigureAwait(false) : 0;
        var permissions = flags.HasFlag(FileAttributeFlags.PERMISSIONS) ? (Permissions)await ReadUInt32(cancellationToken).ConfigureAwait(false) : Permissions.None;
        var atime = flags.HasFlag(FileAttributeFlags.ACMODTIME) ? await ReadTime(cancellationToken).ConfigureAwait(false) : DateTimeOffset.MinValue;
        var mtime = flags.HasFlag(FileAttributeFlags.ACMODTIME) ? await ReadTime(cancellationToken).ConfigureAwait(false) : DateTimeOffset.MinValue;
        var extended_count = flags.HasFlag(FileAttributeFlags.EXTENDED) ? await ReadUInt32(cancellationToken).ConfigureAwait(false) : 0;

        var attrs = new Attributes(size, owner, group, permissions, atime, mtime);

        for (var i = 0; i < extended_count; i++)
        {
            var type = await ReadString(cancellationToken).ConfigureAwait(false);
            var data = await ReadString(cancellationToken).ConfigureAwait(false);
            attrs.ExtendeAttributes.Add(type, data);
        }

        return attrs;
    }

    public async Task<byte[]> ReadBinary(CancellationToken cancellationToken = default)
        => await ReadBinary((int)await ReadUInt32(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);


    private async Task<byte[]> ReadBinary(int length, CancellationToken cancellationToken = default)
    {
        var buffer = new byte[length];
        var offset = 0;
        var bytesread = 0;
        do
        {
            bytesread = await _stream.ReadAsync(buffer.AsMemory(offset, length - offset), cancellationToken).ConfigureAwait(false);
            offset += bytesread;
        } while (!cancellationToken.IsCancellationRequested && bytesread > 0 && offset < length);

        return buffer;
    }
}
