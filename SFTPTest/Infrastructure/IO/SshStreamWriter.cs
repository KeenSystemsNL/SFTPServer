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

    public Task Write(RequestType requestType, CancellationToken cancellationToken = default)
        => Write((byte)requestType, cancellationToken);

    public Task Write(ResponseType responseType, CancellationToken cancellationToken = default)
        => Write((byte)responseType, cancellationToken);

    public Task Write(FileAttributeFlags fileAttributeFlags, CancellationToken cancellationToken = default)
        => Write((uint)fileAttributeFlags, cancellationToken);

    public Task Write(Permissions permissions, CancellationToken cancellationToken = default)
        => Write((uint)permissions, cancellationToken);

    public Task Write(Status status, CancellationToken cancellationToken = default)
        => Write((uint)status, cancellationToken);

    public async Task Write(IReadOnlyCollection<FileSystemInfo> fileSystemInfos, CancellationToken cancellationToken = default)
    {
        await Write(fileSystemInfos.Count, cancellationToken).ConfigureAwait(false);

        foreach (var file in fileSystemInfos)
        {
            await Write(file, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task Write(Attributes attributes, FileAttributeFlags flags = FileAttributeFlags.DEFAULT, CancellationToken cancellationToken = default)
    {
        await Write(flags, cancellationToken).ConfigureAwait(false);
        if (flags.HasFlag(FileAttributeFlags.SIZE))
        {
            await Write(attributes.FileSize, cancellationToken).ConfigureAwait(false);
        }

        if (flags.HasFlag(FileAttributeFlags.UIDGUID))
        {
            await Write(attributes.Uid, cancellationToken).ConfigureAwait(false);
            await Write(attributes.Gid, cancellationToken).ConfigureAwait(false);
        }
        if (flags.HasFlag(FileAttributeFlags.PERMISSIONS))
        {
            await Write(attributes.Permissions, cancellationToken).ConfigureAwait(false);
        }

        if (flags.HasFlag(FileAttributeFlags.ACMODTIME))
        {
            await Write(attributes.LastAccessedTime, cancellationToken).ConfigureAwait(false);
            await Write(attributes.LastModifiedTime, cancellationToken).ConfigureAwait(false);
        }

        if (flags.HasFlag(FileAttributeFlags.EXTENDED))
        {
            await Write(attributes.ExtendeAttributes.Count, cancellationToken).ConfigureAwait(false);
            foreach (var a in attributes.ExtendeAttributes)
            {
                await Write(a.Key, cancellationToken).ConfigureAwait(false);    //type
                await Write(a.Value, cancellationToken).ConfigureAwait(false);  //data
            }
        }
    }

    public async Task Write(DateTimeOffset dateTime, CancellationToken cancellationToken = default)
        => await Write((uint)dateTime.ToUnixTimeSeconds(), cancellationToken).ConfigureAwait(false);

    public async Task Write(FileSystemInfo fileInfo, CancellationToken cancellationToken = default)
    {
        var fileattrs = new Attributes(fileInfo);
        await Write(fileInfo.Name, cancellationToken).ConfigureAwait(false);
        await Write(fileattrs.GetLongFileName(fileInfo.Name), cancellationToken).ConfigureAwait(false);
        await Write(fileattrs, FileAttributeFlags.DEFAULT, cancellationToken).ConfigureAwait(false);
    }


    public Task Write(bool value, CancellationToken cancellationToken = default)
        => Write(value ? (byte)1 : (byte)0, cancellationToken);

    public Task Write(byte value, CancellationToken cancellationToken = default)
        => Write(new[] { value }, cancellationToken);

    public Task Write(int value, CancellationToken cancellationToken = default)
        => Write((uint)value, cancellationToken);

    public Task Write(long value, CancellationToken cancellationToken = default)
        => Write((ulong)value, cancellationToken);

    public Task Write(uint value, CancellationToken cancellationToken = default)
    {
        var bytes = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
        return _memorystream.WriteAsync(bytes, 0, 4, cancellationToken);
    }

    public Task Write(ulong value, CancellationToken cancellationToken = default)
    {
        var bytes = new byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(bytes, value);
        return _memorystream.WriteAsync(bytes, 0, 8, cancellationToken);
    }

    public async Task Write(string str, CancellationToken cancellationToken = default)
    {
        if (str is null)
        {
            throw new ArgumentNullException(nameof(str));
        }
        await Write((uint)str.Length, cancellationToken).ConfigureAwait(false);
        await Write(_encoding.GetBytes(str), cancellationToken).ConfigureAwait(false);
    }

    public Task Write(byte[] data, CancellationToken cancellationToken = default)
        => _memorystream.WriteAsync(data, 0, data.Length, cancellationToken);

    public async Task Flush(ILogger logger, CancellationToken cancellationToken = default)
    {
        var data = _memorystream.ToArray();

        var len = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(len, (uint)data.Length);

        await _stream.WriteAsync(len, cancellationToken).ConfigureAwait(false);
        await _stream.WriteAsync(data, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("HEX  : {data}", string.Join(" ", data.Select(d => d.ToString("X2"))));
        logger.LogInformation("ASCII: {data}", string.Join(" ", data.Select(d => (d >= 32 && d < 127 ? ((char)d).ToString() : ".").PadLeft(2))));

        _memorystream.Position = 0;
        _memorystream.SetLength(0);
    }
}