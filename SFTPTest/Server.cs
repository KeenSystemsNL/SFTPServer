using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SFTPTest.Enums;
using SFTPTest.Infrastructure;
using SFTPTest.Infrastructure.IO;
using System.Diagnostics.CodeAnalysis;

namespace SFTPTest;

public class Server : IServer
{
    private readonly ServerOptions _options;
    private readonly ILogger<Server> _logger;
    private readonly CancellationToken _cancellationtoken;
    private readonly SshStreamReader _reader;
    private readonly SshStreamWriter _writer;
    private readonly Dictionary<string, string> _filehandles = new();
    private readonly Dictionary<string, Stream> _streamhandles = new();
    private uint _protocolversion = 0;

    private readonly Dictionary<RequestType, Func<uint, CancellationToken, Task>> _messagehandlers;

    public Server(IOptions<ServerOptions> options, ILogger<Server> logger, Stream @in, Stream @out, CancellationToken cancellationToken = default)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _reader = new SshStreamReader(@in);
        _writer = new SshStreamWriter(@out, _options.MaxMessageSize);
        _cancellationtoken = cancellationToken;

        _messagehandlers = new()
        {
            { RequestType.REALPATH, RealPathHandler },
            { RequestType.STAT, StatHandler },
            { RequestType.LSTAT, LStatHandler },
            { RequestType.FSTAT, FStatHandler },
            { RequestType.SETSTAT, SetStatHandler },
            { RequestType.FSETSTAT, FSetStatHandler },
            { RequestType.OPENDIR, OpenDirHandler },
            { RequestType.READDIR, ReadDirHandler },
            { RequestType.CLOSE, CloseHandler },
            { RequestType.OPEN, OpenHandler },
            { RequestType.READ, ReadHandler },
            { RequestType.WRITE, WriteHandler },
            { RequestType.REMOVE, RemoveHandler },
            { RequestType.RENAME, RenameHandler },
            { RequestType.MKDIR, MakeDirHandler },
            { RequestType.RMDIR, RemoveDirHandler },
        };
    }

    public async Task Run()
    {
        uint msglength;
        do
        {
            msglength = await _reader.ReadUInt32(_cancellationtoken).ConfigureAwait(false);
            if (msglength > 0)
            {
                // Determine message type
                var msgtype = (RequestType)await _reader.ReadByte(_cancellationtoken).ConfigureAwait(false);
                if (_protocolversion == 0 && msgtype is RequestType.INIT)
                {
                    await InitHandler(_cancellationtoken).ConfigureAwait(false);
                }
                else if (_protocolversion > 0)
                {
                    // Get requestid
                    var requestid = await _reader.ReadUInt32(_cancellationtoken).ConfigureAwait(false);
                    _logger.LogInformation("{msgtype} [ID: {requestid} LEN: {msglength}]", msgtype, requestid, msglength);

                    // Get handler and handle the message when supported
                    if (_messagehandlers.TryGetValue(msgtype, out var handler))
                    {
                        await handler(requestid, _cancellationtoken).ConfigureAwait(false);
                    }
                    else
                    {
                        await SendStatus(requestid, Status.OP_UNSUPPORTED, _cancellationtoken).ConfigureAwait(false);
                    }
                }

                // Write response
                await _writer.Flush(_cancellationtoken).ConfigureAwait(false);
            }
        } while (!_cancellationtoken.IsCancellationRequested && msglength > 0);
    }

    private async Task InitHandler(CancellationToken cancellationToken = default)
    {
        // Get client version
        var clientversion = await _reader.ReadUInt32(cancellationToken).ConfigureAwait(false);
        _protocolversion = Math.Min(clientversion, 3);

        _logger.LogInformation("CLIENT version {clientversion}, sending version {serverversion}", clientversion, _protocolversion);

        // Send version response
        await _writer.Write(RequestType.VERSION, cancellationToken).ConfigureAwait(false);
        await _writer.Write(_protocolversion, cancellationToken).ConfigureAwait(false);
    }

    private async Task RealPathHandler(uint requestid, CancellationToken cancellationToken = default)
    {
        var path = await _reader.ReadString(cancellationToken).ConfigureAwait(false);
        if (path == ".")
        {
            path = "/";
        }

        await _writer.Write(ResponseType.NAME, cancellationToken).ConfigureAwait(false);
        await _writer.Write(requestid, cancellationToken).ConfigureAwait(false);
        await _writer.Write(new[] { new VirtualPath(path) }, cancellationToken).ConfigureAwait(false);
    }

    private Task StatHandler(uint requestid, CancellationToken cancellationToken = default)
        => LStatHandler(requestid, cancellationToken);

    private async Task LStatHandler(uint requestid, CancellationToken cancellationToken = default)
    {
        var path = GetPath(await _reader.ReadString(cancellationToken).ConfigureAwait(false));

        await SendStat(requestid, path, cancellationToken).ConfigureAwait(false);
    }

    private async Task FStatHandler(uint requestid, CancellationToken cancellationToken = default)
    {
        var handle = await _reader.ReadString(cancellationToken).ConfigureAwait(false);

        if (_filehandles.TryGetValue(handle, out var path))
        {
            await SendStat(requestid, path, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await SendInvalidHandle(requestid, cancellationToken).ConfigureAwait(false);
        }
    }

    private Task SendInvalidHandle(uint requestid, CancellationToken cancellationToken = default)
        => SendStatus(requestid, Status.NO_SUCH_FILE, cancellationToken);

    private async Task SetStatHandler(uint requestid, CancellationToken cancellationToken = default)
    {
        var path = GetPath(await _reader.ReadString(cancellationToken).ConfigureAwait(false));
        var attrs = await _reader.ReadAttributes(cancellationToken).ConfigureAwait(false);

        await DoStat(requestid, path, attrs, cancellationToken).ConfigureAwait(false);
        await SendStatus(requestid, Status.OK, cancellationToken).ConfigureAwait(false);
    }

    private async Task FSetStatHandler(uint requestid, CancellationToken cancellationToken = default)
    {
        var handle = await _reader.ReadString(cancellationToken).ConfigureAwait(false);
        var attrs = await _reader.ReadAttributes(cancellationToken).ConfigureAwait(false);

        if (_filehandles.TryGetValue(handle, out var path))
        {
            await DoStat(requestid, path, attrs, cancellationToken).ConfigureAwait(false);
            await SendStatus(requestid, Status.OK, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await SendInvalidHandle(requestid, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task OpenDirHandler(uint requestid, CancellationToken cancellationToken = default)
    {
        var path = GetPath(await _reader.ReadString(cancellationToken).ConfigureAwait(false));

        var handle = GetHandle();
        _filehandles.Add(handle, path);

        await SendHandle(requestid, handle, cancellationToken).ConfigureAwait(false);
    }

    private async Task OpenHandler(uint requestid, CancellationToken cancellationToken = default)
    {
        var filename = await _reader.ReadString(cancellationToken).ConfigureAwait(false);
        var flags = await _reader.ReadAccessFlags(cancellationToken).ConfigureAwait(false);
        var attrs = await _reader.ReadAttributes(cancellationToken).ConfigureAwait(false);

        var handle = GetHandle();
        _streamhandles.Add(handle, File.Open(GetPath(filename), flags.ToFileMode(), flags.ToFileAccess(), FileShare.ReadWrite));
        _filehandles.Add(handle, GetPath(filename));

        await SendHandle(requestid, handle, cancellationToken).ConfigureAwait(false);
    }

    private async Task ReadHandler(uint requestid, CancellationToken cancellationToken = default)
    {
        var handle = await _reader.ReadString(cancellationToken).ConfigureAwait(false);
        var offset = await _reader.ReadUInt64(cancellationToken).ConfigureAwait(false);
        var len = await _reader.ReadUInt32(cancellationToken).ConfigureAwait(false);

        if (_streamhandles.TryGetValue(handle, out var stream))
        {
            if (offset < (ulong)stream.Length)
            {
                stream.Seek((long)offset, SeekOrigin.Begin);
                var buff = new byte[len];
                var bytesread = await stream.ReadAsync(buff.AsMemory(0, (int)len), cancellationToken).ConfigureAwait(false);

                await _writer.Write(ResponseType.DATA, cancellationToken).ConfigureAwait(false);
                await _writer.Write(requestid, cancellationToken).ConfigureAwait(false);
                await _writer.Write(bytesread, cancellationToken).ConfigureAwait(false);
                await _writer.Write(buff[..bytesread], cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await SendStatus(requestid, Status.EOF, cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            await SendInvalidHandle(requestid, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task WriteHandler(uint requestid, CancellationToken cancellationToken = default)
    {
        var handle = await _reader.ReadString(cancellationToken).ConfigureAwait(false);
        var offset = (long)await _reader.ReadUInt64(cancellationToken).ConfigureAwait(false);
        var data = await _reader.ReadBinary(cancellationToken).ConfigureAwait(false);

        if (_streamhandles.TryGetValue(handle, out var stream))
        {
            if (stream.Position != offset)
            {
                stream.Seek(offset, SeekOrigin.Begin);
            }
            await stream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
            await SendStatus(requestid, Status.OK, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await SendInvalidHandle(requestid, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ReadDirHandler(uint requestid, CancellationToken cancellationToken = default)
    {
        var handle = await _reader.ReadString(cancellationToken).ConfigureAwait(false);

        if (_filehandles.TryGetValue(handle, out var path))
        {
            await _writer.Write(ResponseType.NAME, cancellationToken).ConfigureAwait(false);
            await _writer.Write(requestid, cancellationToken).ConfigureAwait(false);
            await _writer.Write(new DirectoryInfo(path).GetFileSystemInfos().OrderBy(f => f.Name).ToArray(), cancellationToken).ConfigureAwait(false);

            _filehandles.Remove(handle);
        }
        else
        {
            await SendStatus(requestid, Status.EOF, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task CloseHandler(uint requestId, CancellationToken cancellationToken = default)
    {
        var handle = await _reader.ReadString(cancellationToken).ConfigureAwait(false);

        _filehandles.Remove(handle);

        if (_streamhandles.TryGetValue(handle, out var stream))
        {
            stream.Close();
            stream.Dispose();
        }
        _streamhandles.Remove(handle);

        await SendStatus(requestId, Status.OK, cancellationToken).ConfigureAwait(false);
    }

    private async Task RemoveHandler(uint requestid, CancellationToken cancellationToken = default)
    {
        var filename = GetPath(await _reader.ReadString(cancellationToken).ConfigureAwait(false));

        if (TryGetFSObject(filename, out var fsObject) && fsObject is FileInfo)
        {
            File.Delete(fsObject.FullName);
            await SendStatus(requestid, Status.OK, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await SendStatus(requestid, Status.FAILURE, cancellationToken).ConfigureAwait(false);  // TODO: Return (more) correct status like NO_SUCH_FILE or PERMISSION_DENIED etc.
        }
    }

    private async Task RenameHandler(uint requestid, CancellationToken cancellationToken = default)
    {
        var oldfilename = GetPath(await _reader.ReadString(cancellationToken).ConfigureAwait(false));
        var newfilename = GetPath(await _reader.ReadString(cancellationToken).ConfigureAwait(false));

        if (TryGetFSObject(oldfilename, out var fsOldObject) && fsOldObject is FileInfo)
        {
            File.Move(fsOldObject.FullName, newfilename);
            await SendStatus(requestid, Status.OK, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await SendStatus(requestid, Status.FAILURE, cancellationToken).ConfigureAwait(false);  // TODO: Return (more) correct status like NO_SUCH_FILE or PERMISSION_DENIED etc.
        }
    }

    private async Task MakeDirHandler(uint requestid, CancellationToken cancellationToken = default)
    {
        var name = GetPath(await _reader.ReadString(cancellationToken).ConfigureAwait(false));
        var attrs = _reader.ReadAttributes(cancellationToken);

        if (!TryGetFSObject(name, out var fsObject))
        {
            Directory.CreateDirectory(name);
            await SendStatus(requestid, Status.OK, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await SendStatus(requestid, Status.FAILURE, cancellationToken).ConfigureAwait(false);  // TODO: Return (more) correct status like NO_SUCH_FILE or PERMISSION_DENIED etc.
        }
    }

    private async Task RemoveDirHandler(uint requestid, CancellationToken cancellationToken = default)
    {
        var name = GetPath(await _reader.ReadString(cancellationToken).ConfigureAwait(false));
        if (TryGetFSObject(name, out var fsObject) && fsObject is DirectoryInfo)
        {
            Directory.Delete(name);
            await SendStatus(requestid, Status.OK, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await SendStatus(requestid, Status.FAILURE, cancellationToken).ConfigureAwait(false);  // TODO: Return (more) correct status like NO_SUCH_FILE or PERMISSION_DENIED etc.
        }
    }


    private static Task DoStat(uint requestid, string path, Attributes attributes, CancellationToken cancellationToken = default)
    {
        if (attributes.LastModifiedTime != DateTimeOffset.MinValue)
        {
            File.SetLastWriteTimeUtc(path, attributes.LastModifiedTime.UtcDateTime);
        }
        //TODO: implement permissions??

        return Task.CompletedTask;
    }

    private async Task SendHandle(uint requestId, string handle, CancellationToken cancellationToken = default)
    {
        await _writer.Write(ResponseType.HANDLE, cancellationToken).ConfigureAwait(false);
        await _writer.Write(requestId, cancellationToken).ConfigureAwait(false);
        await _writer.Write(handle, cancellationToken).ConfigureAwait(false);
    }

    private async Task SendStat(uint requestid, string path, CancellationToken cancellationToken = default)
    {
        if (TryGetFSObject(path, out var fso))
        {
            await _writer.Write(ResponseType.ATTRS, cancellationToken).ConfigureAwait(false);
            await _writer.Write(requestid, cancellationToken).ConfigureAwait(false);
            await _writer.Write(new Attributes(fso), FileAttributeFlags.DEFAULT, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await SendStatus(requestid, Status.NO_SUCH_FILE, cancellationToken).ConfigureAwait(false);
        }
    }

    private Task SendStatus(uint requestId, Status status, CancellationToken cancellationToken = default)
        => SendStatus(requestId, status, GetStatusString(status), string.Empty, cancellationToken);

    private async Task SendStatus(uint requestId, Status status, string errorMessage, string languageTag, CancellationToken cancellationToken = default)
    {
        await _writer.Write(ResponseType.STATUS, cancellationToken).ConfigureAwait(false);
        await _writer.Write(requestId, cancellationToken).ConfigureAwait(false);
        await _writer.Write(status, cancellationToken).ConfigureAwait(false);
        await _writer.Write(errorMessage, cancellationToken).ConfigureAwait(false);
        await _writer.Write(languageTag, cancellationToken).ConfigureAwait(false);
    }

    private static string GetStatusString(Status status)
        => status switch
        {
            Status.OK => "Succes",
            Status.EOF => "End of file",
            Status.NO_SUCH_FILE => "No such file",
            Status.PERMISSION_DENIED => "Permission denied",
            Status.FAILURE => "Failure",
            Status.BAD_MESSAGE => "Bad message",
            Status.NO_CONNECTION => "No connection",
            Status.CONNECTION_LOST => "Connection lost",
            Status.OP_UNSUPPORTED => "Operation unsupported",
            _ => "Unknown error"
        };

    private static bool TryGetFSObject(string path, [NotNullWhen(true)] out FileSystemInfo? fileSystemObject)
    {
        if (Directory.Exists(path))
        {
            fileSystemObject = new DirectoryInfo(path);
            return true;
        }
        if (File.Exists(path))
        {
            fileSystemObject = new FileInfo(path);
            return true;
        }
        fileSystemObject = null;
        return false;
    }

    private static string GetHandle()
        => Guid.NewGuid().ToString("N");

    private string GetPath(string path)
    {
        var result = Path.GetFullPath(Path.Combine(_options.Root, path.TrimStart('/'))).Replace('/', '\\');
        return result.StartsWith(_options.Root) ? result : _options.Root;
    }
}