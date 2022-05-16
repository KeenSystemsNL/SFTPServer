using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SFTPTest.Enums;
using SFTPTest.Infrastructure;
using SFTPTest.Infrastructure.IO;

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

    public void Run(Stream @in, Stream @out)
    {
        var reader = new SshStreamReader(@in);
        var writer = new SshStreamWriter(@out, _options.MaxMessageSize);
        Session? session = null;
        uint msglength;
        do
        {
            msglength = reader.ReadUInt32();
            if (msglength > 0)
            {
                // Determine message type
                var msgtype = (RequestType)reader.ReadByte();
                if (session is null && msgtype is RequestType.INIT)
                {
                    session = InitHandler(reader, writer);
                }
                else if (session is not null)
                {
                    // Get requestid
                    var requestid = reader.ReadUInt32();
                    _logger.LogInformation("{msgtype} [{request}]", msgtype, Dumper.Dump(requestid));
                    if (!_messagehandlers.TryGetValue(msgtype, out var handler))
                    {
                        SendStatus(session, requestid, Status.OP_UNSUPPORTED);
                    }
                    else
                    {
                        handler(session, requestid);
                    }

                }

                // Write response
                writer.Flush(_logger);
            }
        } while (msglength > 0);
    }

    private Session InitHandler(SshStreamReader reader, SshStreamWriter writer)
    {
        // Get client version
        var clientversion = reader.ReadUInt32();

        var version = Math.Min(clientversion, 4);

        // Send version response
        writer.Write(RequestType.VERSION);
        writer.Write(version);
        return new Session(reader, writer, new FileHandleCollection(), new FileStreamCollection(), version, _options.Root, _logger);
    }

    private static void RealPathHandler(Session session, uint requestid)
    {
        var path = session.Reader.ReadString();
        if (path == ".")
        {
            path = "/";
        }

        session.Logger.LogInformation("Path: {path}", path);
        session.Writer.Write(ResponseType.NAME);
        session.Writer.Write(requestid);
        session.Writer.Write(1);
        session.Writer.Write(new VirtualPath(path));
    }

    private static void StatHandler(Session session, uint requestid)
        => LStatHandler(session, requestid);

    private static void LStatHandler(Session session, uint requestid)
    {
        var path = session.Reader.ReadString();
        var flags = session.Reader.ReadFileAttributeFlags();

        SendStat(session, requestid, path, flags);
    }

    private static void FStatHandler(Session session, uint requestid)
    {
        var handle = session.Reader.ReadString();
        var flags = session.Reader.ReadFileAttributeFlags();

        if (session.FileHandles.TryGetValue(handle, out var path))
        {
            SendStat(session, requestid, path, flags);
        }
        else
        {
            SendStatus(session, requestid, Status.INVALID_HANDLE);
        }
    }

    private static void SetStatHandler(Session session, uint requestid)
    {
        var path = GetPath(session, session.Reader.ReadString());
        var attrs = session.Reader.ReadAttributes();

        DoStat(session, requestid, path, attrs);
    }

    private static void FSetStatHandler(Session session, uint requestid)
    {
        var handle = session.Reader.ReadString();
        var attrs = session.Reader.ReadAttributes();

        if (session.FileHandles.TryGetValue(handle, out var path))
        {
            DoStat(session, requestid, path, attrs);
        }
        else
        {
            SendStatus(session, requestid, Status.INVALID_HANDLE);
        }
    }

    private static void OpenDirHandler(Session session, uint requestid)
    {
        var path = session.Reader.ReadString();

        var handle = GetHandle();
        session.FileHandles.Add(handle, GetPath(session, path));

        session.Logger.LogInformation("Path: {path}, Handle: {handle}", path, handle);
        SendHandle(session, requestid, handle);
    }

    private static void OpenHandler(Session session, uint requestid)
    {
        var filename = session.Reader.ReadString();
        var flags = session.Reader.ReadAccessFlags();
        var attrs = session.Reader.ReadAttributes();

        var handle = GetHandle();
        session.FileStreams.Add(handle, File.Open(GetPath(session, filename), flags.ToFileMode(), flags.ToFileAccess(), FileShare.ReadWrite));
        session.FileHandles.Add(handle, GetPath(session, filename));

        session.Logger.LogInformation("File: {filename}, Flags: {flags}, Attrs: {attrs}", filename, flags, attrs);
        SendHandle(session, requestid, handle);
    }

    private static void ReadHandler(Session session, uint requestid)
    {
        var handle = session.Reader.ReadString();
        var offset = session.Reader.ReadUInt64();
        var len = session.Reader.ReadUInt32();

        if (session.FileStreams.TryGetValue(handle, out var stream))
        {
            if (offset < (ulong)stream.Length)
            {
                stream.Seek((long)offset, SeekOrigin.Begin);
                var buff = new byte[len];
                var bytesread = stream.Read(buff, 0, (int)len);

                session.Writer.Write(ResponseType.DATA);
                session.Writer.Write(requestid);
                session.Writer.Write(bytesread);
                session.Writer.Write(buff.AsSpan()[..bytesread]);
            }
            else
            {
                SendStatus(session, requestid, Status.EOF);
            }
        }
        else
        {
            SendStatus(session, requestid, Status.INVALID_HANDLE);
        }
    }

    private static void WriteHandler(Session session, uint requestid)
    {
        var handle = session.Reader.ReadString();
        var offset = session.Reader.ReadUInt64();

        session.Logger.LogInformation("Write {handle} from {offset}", handle, offset);
        if (session.FileStreams.TryGetValue(handle, out var stream))
        {
            var data = session.Reader.ReadBinary();

            stream.Seek((long)offset, SeekOrigin.Begin);
            stream.Write(data, 0, data.Length);
        }
        else
        {
            SendStatus(session, requestid, Status.INVALID_HANDLE);
        }
    }

    private static void ReadDirHandler(Session session, uint requestid)
    {
        var handle = session.Reader.ReadString();

        if (session.FileHandles.TryGetValue(handle, out var path))
        {
            session.Logger.LogInformation("Path: {path}, Handle: {handle}", path, handle);

            var allfiles = new DirectoryInfo(path).GetFileSystemInfos().OrderBy(f => f.Name).ToArray();

            session.Writer.Write(ResponseType.NAME);
            session.Writer.Write(requestid);
            session.Writer.Write(allfiles.Length); // all files at the same time

            foreach (var file in allfiles)
            {
                session.Writer.Write(file);
            }
            session.Writer.Write(true); // End of list

            session.FileHandles.Remove(handle);
        }
        else
        {
            SendStatus(session, requestid, Status.EOF);
        }
    }

    private static void CloseHandler(Session session, uint requestId)
    {
        var handle = session.Reader.ReadString();

        session.Logger.LogInformation("Handle: {handle}", handle);

        session.FileHandles.Remove(handle);

        if (session.FileStreams.TryGetValue(handle, out var stream))
        {
            stream.Close();
            stream.Dispose();
        }
        session.FileStreams.Remove(handle);

        SendStatus(session, requestId, Status.OK);
    }

    private static void RemoveHandler(Session session, uint requestid)
    {
        var filename = GetPath(session, session.Reader.ReadString());

        session.Logger.LogInformation("DELETE: {filename}", filename);
        try
        {
            File.Delete(filename);
            SendStatus(session, requestid, Status.OK);
        }
        catch
        {
            SendStatus(session, requestid, Status.FAILURE);  // TODO: Return (more) correct status like NO_SUCH_FILE or PERMISSION_DENIED etc.
        }
    }

    private static void RenameHandler(Session session, uint requestid)
    {
        var oldfilename = GetPath(session, session.Reader.ReadString());
        var newfilename = GetPath(session, session.Reader.ReadString());

        session.Logger.LogInformation("RENAME: {oldfilename} -> {newfilename}", oldfilename, newfilename);
        try
        {
            File.Move(oldfilename, newfilename);
            SendStatus(session, requestid, Status.OK);
        }
        catch
        {
            SendStatus(session, requestid, Status.FAILURE);  // TODO: Return (more) correct status like NO_SUCH_FILE or PERMISSION_DENIED etc.
        }
    }

    private static void MakeDirHandler(Session session, uint requestid)
    {
        var name = GetPath(session, session.Reader.ReadString());
        var attrs = session.Reader.ReadAttributes();

        session.Logger.LogInformation("MAKEDIR: {name} [{attributes}]", name, attrs);
        try
        {
            Directory.CreateDirectory(name);
            SendStatus(session, requestid, Status.OK);
        }
        catch
        {
            SendStatus(session, requestid, Status.FAILURE);  // TODO: Return (more) correct status like NO_SUCH_FILE or PERMISSION_DENIED etc.
        }
    }

    private static void RemoveDirHandler(Session session, uint requestid)
    {
        var name = GetPath(session, session.Reader.ReadString());
        session.Logger.LogInformation("REMOVEDIR: {name}", name);
        try
        {
            Directory.Delete(name);
            SendStatus(session, requestid, Status.OK);
        }
        catch
        {
            SendStatus(session, requestid, Status.FAILURE);  // TODO: Return (more) correct status like NO_SUCH_FILE or PERMISSION_DENIED etc.
        }
    }


    private static void DoStat(Session session, uint requestid, string path, Attributes attributes)
    {
        session.Logger.LogInformation("Stat: {path}, attributes: {flags}", path, attributes);
        //TODO: implement
        SendStatus(session, requestid, Status.OK);
    }

    private static void SendHandle(Session session, uint requestId, string handle)
    {
        session.Writer.Write(ResponseType.HANDLE);
        session.Writer.Write(requestId);
        session.Writer.Write(handle);
    }

    private static void SendStat(Session session, uint requestid, string path, FileAttributeFlags flags)
    {
        try
        {
            session.Writer.Write(ResponseType.ATTRS);
            session.Writer.Write(requestid);
            session.Writer.Write(new Attributes(new FileInfo(path)), flags);
        }
        catch
        {
            SendStatus(session, requestid, Status.FAILURE);
        }
    }

    private static void SendStatus(Session session, uint requestId, Status status)
        => SendStatus(session, requestId, status, GetStatusString(status), string.Empty);

    private static void SendStatus(Session session, uint requestId, Status status, string errorMessage, string languageTag)
    {
        session.Writer.Write(ResponseType.STATUS);
        session.Writer.Write(requestId);
        session.Writer.Write(status);
        session.Writer.Write(errorMessage);
        session.Writer.Write(languageTag);
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

    private static string GetHandle()
        => Guid.NewGuid().ToString("N");

    private static string GetPath(Session session, string path)
    {
        var result = Path.GetFullPath(Path.Combine(session.Root, path.TrimStart('/'))).Replace('/', '\\');
        return result.StartsWith(session.Root) ? result : session.Root;
    }
}