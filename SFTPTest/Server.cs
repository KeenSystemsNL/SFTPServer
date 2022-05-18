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

    private static readonly MessageHandlerCollection _messagehandlers = new()
    {
        //{ MessageType.INIT, InitHandler }, // Handled separately
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

    public Server(IOptions<ServerOptions> options, ILogger<Server> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Run(Stream @in, Stream @out, CancellationToken cancellationToken)
    {
        var reader = new SshStreamReader(@in);
        var writer = new SshStreamWriter(@out, _options.MaxMessageSize);
        Session? session = null;
        uint msglength;
        do
        {
            msglength = await reader.ReadUInt32(cancellationToken).ConfigureAwait(false);
            if (msglength > 0)
            {
                // Determine message type
                var msgtype = (RequestType)await reader.ReadByte(cancellationToken).ConfigureAwait(false);
                if (session is null && msgtype is RequestType.INIT)
                {
                    session = await InitHandler(reader, writer, cancellationToken).ConfigureAwait(false);
                }
                else if (session is not null)
                {
                    // Get requestid
                    var requestid = await reader.ReadUInt32(cancellationToken).ConfigureAwait(false);
                    _logger.LogInformation("=== {msgtype} [{msglength}] ===", msgtype, msglength);

                    // Get handler and handle the message when supported
                    if (_messagehandlers.TryGetValue(msgtype, out var handler))
                    {
                        await handler(session, requestid, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        await SendStatus(session, requestid, Status.OP_UNSUPPORTED, cancellationToken).ConfigureAwait(false);
                    }
                }

                // Write response
                await writer.Flush(_logger, cancellationToken).ConfigureAwait(false);
            }
        } while (!cancellationToken.IsCancellationRequested && msglength > 0);
        _logger.LogInformation("Cancel: {cancel}, EOF: {eof}", cancellationToken.IsCancellationRequested, msglength > 0);
    }

    private async Task<Session> InitHandler(SshStreamReader reader, SshStreamWriter writer, CancellationToken cancellationToken = default)
    {
        // Get client version
        var clientversion = await reader.ReadUInt32(cancellationToken).ConfigureAwait(false);
        var version = Math.Min(clientversion, 3);

        _logger.LogInformation("CLIENT version {clientversion}, sending version {serverversion}", clientversion, version);


        // Send version response
        await writer.Write(RequestType.VERSION, cancellationToken).ConfigureAwait(false);
        await writer.Write(version, cancellationToken).ConfigureAwait(false);
        return new Session(reader, writer, new FileHandleCollection(), new FileStreamCollection(), version, _options.Root, _logger);
    }

    private static async Task RealPathHandler(Session session, uint requestid, CancellationToken cancellationToken = default)
    {
        var path = await session.Reader.ReadString(cancellationToken).ConfigureAwait(false);
        if (path == ".")
        {
            path = "/";
        }

        session.Logger.LogInformation("Path: {path}", GetPath(session, path));
        await session.Writer.Write(ResponseType.NAME, cancellationToken).ConfigureAwait(false);
        await session.Writer.Write(requestid, cancellationToken).ConfigureAwait(false);
        await session.Writer.Write(new[] { new VirtualPath(path) }, cancellationToken).ConfigureAwait(false);
    }

    private static Task StatHandler(Session session, uint requestid, CancellationToken cancellationToken = default)
        => LStatHandler(session, requestid, cancellationToken);

    private static async Task LStatHandler(Session session, uint requestid, CancellationToken cancellationToken = default)
    {
        var path = GetPath(session, await session.Reader.ReadString(cancellationToken).ConfigureAwait(false));

        session.Logger.LogInformation("Sending STAT for {path}", path);
        await SendStat(session, requestid, path, cancellationToken).ConfigureAwait(false);
    }

    private static async Task FStatHandler(Session session, uint requestid, CancellationToken cancellationToken = default)
    {
        var handle = await session.Reader.ReadString(cancellationToken).ConfigureAwait(false);

        if (session.FileHandles.TryGetValue(handle, out var path))
        {
            await SendStat(session, requestid, path, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await SendInvalidHandle(session, requestid, cancellationToken).ConfigureAwait(false);
        }
    }

    private static Task SendInvalidHandle(Session session, uint requestid, CancellationToken cancellationToken = default)
        => SendStatus(session, requestid, Status.NO_SUCH_FILE, cancellationToken);

    private static async Task SetStatHandler(Session session, uint requestid, CancellationToken cancellationToken = default)
    {
        var path = GetPath(session, await session.Reader.ReadString(cancellationToken).ConfigureAwait(false));
        var attrs = await session.Reader.ReadAttributes(cancellationToken).ConfigureAwait(false);

        await DoStat(session, requestid, path, attrs, cancellationToken).ConfigureAwait(false);
        await SendStatus(session, requestid, Status.OK, cancellationToken).ConfigureAwait(false);
    }

    private static async Task FSetStatHandler(Session session, uint requestid, CancellationToken cancellationToken = default)
    {
        var handle = await session.Reader.ReadString(cancellationToken).ConfigureAwait(false);
        var attrs = await session.Reader.ReadAttributes(cancellationToken).ConfigureAwait(false);

        if (session.FileHandles.TryGetValue(handle, out var path))
        {
            await DoStat(session, requestid, path, attrs, cancellationToken).ConfigureAwait(false);
            await SendStatus(session, requestid, Status.OK, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await SendInvalidHandle(session, requestid, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task OpenDirHandler(Session session, uint requestid, CancellationToken cancellationToken = default)
    {
        var path = GetPath(session, await session.Reader.ReadString(cancellationToken).ConfigureAwait(false));

        var handle = GetHandle();
        session.FileHandles.Add(handle, path);

        session.Logger.LogInformation("Path: {path}, Handle: {handle}", path, handle);
        await SendHandle(session, requestid, handle, cancellationToken).ConfigureAwait(false);
    }

    private static async Task OpenHandler(Session session, uint requestid, CancellationToken cancellationToken = default)
    {
        var filename = await session.Reader.ReadString(cancellationToken).ConfigureAwait(false);
        var flags = await session.Reader.ReadAccessFlags(cancellationToken).ConfigureAwait(false);
        var attrs = await session.Reader.ReadAttributes(cancellationToken).ConfigureAwait(false);

        var handle = GetHandle();
        session.FileStreams.Add(handle, File.Open(GetPath(session, filename), flags.ToFileMode(), flags.ToFileAccess(), FileShare.ReadWrite));
        session.FileHandles.Add(handle, GetPath(session, filename));

        session.Logger.LogInformation("File: {filename}, Flags: {flags}, Attrs: {attrs}", filename, flags, attrs);
        await SendHandle(session, requestid, handle, cancellationToken).ConfigureAwait(false);
    }

    private static async Task ReadHandler(Session session, uint requestid, CancellationToken cancellationToken = default)
    {
        var handle = await session.Reader.ReadString(cancellationToken).ConfigureAwait(false);
        var offset = await session.Reader.ReadUInt64(cancellationToken).ConfigureAwait(false);
        var len = await session.Reader.ReadUInt32(cancellationToken).ConfigureAwait(false);

        if (session.FileStreams.TryGetValue(handle, out var stream))
        {
            if (offset < (ulong)stream.Length)
            {
                stream.Seek((long)offset, SeekOrigin.Begin);
                var buff = new byte[len];
                var bytesread = await stream.ReadAsync(buff.AsMemory(0, (int)len), cancellationToken).ConfigureAwait(false);

                await session.Writer.Write(ResponseType.DATA, cancellationToken).ConfigureAwait(false);
                await session.Writer.Write(requestid, cancellationToken).ConfigureAwait(false);
                await session.Writer.Write(bytesread, cancellationToken).ConfigureAwait(false);
                await session.Writer.Write(buff[..bytesread], cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await SendStatus(session, requestid, Status.EOF, cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            await SendInvalidHandle(session, requestid, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task WriteHandler(Session session, uint requestid, CancellationToken cancellationToken = default)
    {
        var handle = await session.Reader.ReadString(cancellationToken).ConfigureAwait(false);
        var offset = (long)await session.Reader.ReadUInt64(cancellationToken).ConfigureAwait(false);
        var data = await session.Reader.ReadBinary(cancellationToken).ConfigureAwait(false);

        session.Logger.LogInformation("Write {handle} from {offset}, {length} bytes", handle, offset, data.Length);
        if (session.FileStreams.TryGetValue(handle, out var stream))
        {
            if (stream.Position != offset)
            {
                stream.Seek(offset, SeekOrigin.Begin);
            }
            await stream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
            await SendStatus(session, requestid, Status.OK, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await SendInvalidHandle(session, requestid, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task ReadDirHandler(Session session, uint requestid, CancellationToken cancellationToken = default)
    {
        var handle = await session.Reader.ReadString(cancellationToken).ConfigureAwait(false);

        if (session.FileHandles.TryGetValue(handle, out var path))
        {
            session.Logger.LogInformation("Path: {path}, Handle: {handle}", path, handle);

            await session.Writer.Write(ResponseType.NAME, cancellationToken).ConfigureAwait(false);
            await session.Writer.Write(requestid, cancellationToken).ConfigureAwait(false);
            await session.Writer.Write(new DirectoryInfo(path).GetFileSystemInfos().OrderBy(f => f.Name).ToArray(), cancellationToken).ConfigureAwait(false);

            session.FileHandles.Remove(handle);
        }
        else
        {
            await SendStatus(session, requestid, Status.EOF, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task CloseHandler(Session session, uint requestId, CancellationToken cancellationToken = default)
    {
        var handle = await session.Reader.ReadString(cancellationToken).ConfigureAwait(false);

        session.Logger.LogInformation("Handle: {handle}", handle);

        session.FileHandles.Remove(handle);

        if (session.FileStreams.TryGetValue(handle, out var stream))
        {
            stream.Close();
            stream.Dispose();
        }
        session.FileStreams.Remove(handle);

        await SendStatus(session, requestId, Status.OK, cancellationToken).ConfigureAwait(false);
    }

    private static async Task RemoveHandler(Session session, uint requestid, CancellationToken cancellationToken = default)
    {
        var filename = GetPath(session, await session.Reader.ReadString(cancellationToken).ConfigureAwait(false));

        session.Logger.LogInformation("DELETE: {filename}", filename);
        if (TryGetFSObject(filename, out var fsObject) && fsObject is FileInfo)
        {
            File.Delete(fsObject.FullName);
            await SendStatus(session, requestid, Status.OK, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await SendStatus(session, requestid, Status.FAILURE, cancellationToken).ConfigureAwait(false);  // TODO: Return (more) correct status like NO_SUCH_FILE or PERMISSION_DENIED etc.
        }
    }

    private static async Task RenameHandler(Session session, uint requestid, CancellationToken cancellationToken = default)
    {
        var oldfilename = GetPath(session, await session.Reader.ReadString(cancellationToken).ConfigureAwait(false));
        var newfilename = GetPath(session, await session.Reader.ReadString(cancellationToken).ConfigureAwait(false));

        session.Logger.LogInformation("RENAME: {oldfilename} -> {newfilename}", oldfilename, newfilename);
        if (TryGetFSObject(oldfilename, out var fsOldObject) && fsOldObject is FileInfo)
        {
            File.Move(fsOldObject.FullName, newfilename);
            await SendStatus(session, requestid, Status.OK, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await SendStatus(session, requestid, Status.FAILURE, cancellationToken).ConfigureAwait(false);  // TODO: Return (more) correct status like NO_SUCH_FILE or PERMISSION_DENIED etc.
        }
    }

    private static async Task MakeDirHandler(Session session, uint requestid, CancellationToken cancellationToken = default)
    {
        var name = GetPath(session, await session.Reader.ReadString(cancellationToken).ConfigureAwait(false));
        session.Logger.LogInformation("MAKEDIR {name}", name);
        var attrs = session.Reader.ReadAttributes(cancellationToken);

        session.Logger.LogInformation("MAKEDIR: {name} [{attributes}]", name, attrs);
        if (!TryGetFSObject(name, out var fsObject))
        {
            Directory.CreateDirectory(name);
            await SendStatus(session, requestid, Status.OK, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await SendStatus(session, requestid, Status.FAILURE, cancellationToken).ConfigureAwait(false);  // TODO: Return (more) correct status like NO_SUCH_FILE or PERMISSION_DENIED etc.
        }
    }

    private static async Task RemoveDirHandler(Session session, uint requestid, CancellationToken cancellationToken = default)
    {
        var name = GetPath(session, await session.Reader.ReadString(cancellationToken).ConfigureAwait(false));
        session.Logger.LogInformation("REMOVEDIR: {name}", name);
        if (TryGetFSObject(name, out var fsObject) && fsObject is DirectoryInfo)
        {
            Directory.Delete(name);
            await SendStatus(session, requestid, Status.OK, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await SendStatus(session, requestid, Status.FAILURE, cancellationToken).ConfigureAwait(false);  // TODO: Return (more) correct status like NO_SUCH_FILE or PERMISSION_DENIED etc.
        }
    }


    private static Task DoStat(Session session, uint requestid, string path, Attributes attributes, CancellationToken cancellationToken = default)
    {
        session.Logger.LogInformation("Stat: {path}, attributes: {flags}", path, attributes);

        //if (attributes.LastModifiedTime != DateTimeOffset.MinValue)
        //{
        //    session.Logger.LogInformation("Setting MTime {path} to {mtime}", path, attributes.LastModifiedTime.UtcDateTime);
        //    File.SetLastWriteTimeUtc(path, attributes.LastModifiedTime.UtcDateTime);
        //}

        //TODO: implement
        return Task.CompletedTask;
    }

    private static async Task SendHandle(Session session, uint requestId, string handle, CancellationToken cancellationToken = default)
    {
        await session.Writer.Write(ResponseType.HANDLE, cancellationToken).ConfigureAwait(false);
        await session.Writer.Write(requestId, cancellationToken).ConfigureAwait(false);
        await session.Writer.Write(handle, cancellationToken).ConfigureAwait(false);
    }

    private static async Task SendStat(Session session, uint requestid, string path, CancellationToken cancellationToken = default)
    {
        if (TryGetFSObject(path, out var fso))
        {
            await session.Writer.Write(ResponseType.ATTRS, cancellationToken).ConfigureAwait(false);
            await session.Writer.Write(requestid, cancellationToken).ConfigureAwait(false);
            await session.Writer.Write(new Attributes(fso), FileAttributeFlags.DEFAULT, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await SendStatus(session, requestid, Status.NO_SUCH_FILE, cancellationToken).ConfigureAwait(false);
        }
    }

    private static Task SendStatus(Session session, uint requestId, Status status, CancellationToken cancellationToken = default)
        => SendStatus(session, requestId, status, GetStatusString(status), string.Empty, cancellationToken);

    private static async Task SendStatus(Session session, uint requestId, Status status, string errorMessage, string languageTag, CancellationToken cancellationToken = default)
    {
        await session.Writer.Write(ResponseType.STATUS, cancellationToken).ConfigureAwait(false);
        await session.Writer.Write(requestId, cancellationToken).ConfigureAwait(false);
        await session.Writer.Write(status, cancellationToken).ConfigureAwait(false);
        await session.Writer.Write(errorMessage, cancellationToken).ConfigureAwait(false);
        await session.Writer.Write(languageTag, cancellationToken).ConfigureAwait(false);
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

    private static string GetPath(Session session, string path)
    {
        var result = Path.GetFullPath(Path.Combine(session.Root, path.TrimStart('/'))).Replace('/', '\\');
        return result.StartsWith(session.Root) ? result : session.Root;
    }
}