using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SFTPTest.Infrastructure;
using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace SFTPTest;

public class Server : IServer
{
    private readonly ServerOptions _options;
    private readonly ILogger<Server> _logger;

    private static readonly MessageHandlerCollection _messagehandlers = new()
    {
        //{ MessageType.INIT, InitHandler }, // Handled separately
        { MessageType.REALPATH, RealPathHandler },
        { MessageType.STAT, StatHandler },
        { MessageType.LSTAT, LStatHandler },
        { MessageType.OPENDIR, OpenDirHandler },
        { MessageType.READDIR, ReadDirHandler },
        { MessageType.CLOSE, CloseHandler },
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
        var session = new Session(reader, writer, new FileHandleCollection());
        uint msglength;
        var initdone = false;
        do
        {
            msglength = reader.ReadUInt32();
            if (msglength > 0)
            {
                // Determine message type
                var msgtype = (MessageType)reader.ReadByte();
                if (!initdone && msgtype == MessageType.INIT)
                {
                    InitHandler(session, 0);
                    initdone = true;
                }
                else if (initdone)
                {
                    // Get requestid
                    var requestid = reader.ReadUInt32();
                    _logger.LogInformation("{msgtype} [{request}]", msgtype, Dumper.Dump(requestid));
                    if (!_messagehandlers.TryGetValue(msgtype, out var handler))
                    {
                        SendStatus(session.Writer, requestid, Status.OP_UNSUPPORTED);
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

    private static void InitHandler(Session session, uint requestid)
    {
        // Get client version
        var clientversion = session.Reader.ReadUInt32();
        // Send version response (v3)
        session.Writer.Write(MessageType.VERSION);
        session.Writer.Write((uint)3);
    }

    private static void RealPathHandler(Session session, uint requestid)
    {
        var path = session.Reader.ReadString();
        //var controlbyte = reader.ReadByte();
        //_logger.LogInformation("Control byte: {path}", path);
        //var composepath = reader.ReadString(Encoding.UTF8);
        //_logger.LogInformation("Compose path: {composepath}", composepath);

        session.Writer.Write(MessageType.NAME);
        session.Writer.Write(requestid);
        session.Writer.Write((uint)1);
        // Dummy file for SSH_FXP_REALPATH request
        session.Writer.Write(path);
        session.Writer.Write($@"----------   0 nobody   nobody          0 {DateTime.Now.ToString("MMM dd HH:mm", CultureInfo.InvariantCulture)} " + path);
        session.Writer.Write(requestid);
        session.Writer.Write(uint.MaxValue); // flags
        session.Writer.Write((ulong)0); // size
        session.Writer.Write(uint.MaxValue); // uid
        session.Writer.Write(uint.MaxValue); // gid
        session.Writer.Write(uint.MaxValue); // permissions
        session.Writer.Write(GetUnixFileTime(DateTime.Now)); //atime   
        session.Writer.Write(GetUnixFileTime(DateTime.Now)); //mtime
        session.Writer.Write((uint)0); // extended_count
    }

    private static void StatHandler(Session session, uint requestid)
        => LStatHandler(session, requestid);

    private static void LStatHandler(Session session, uint requestid)
    {
        var path = session.Reader.ReadString();

        session.Writer.Write(MessageType.ATTRS);
        session.Writer.Write(requestid);

        SendAttributes(session.Writer, new Attributes(0));
    }

    private static void OpenDirHandler(Session session, uint requestid)
    {
        var path = session.Reader.ReadString();

        var handle = GetHandle();
        session.FileHandles.Add(handle, path);

        session.Writer.Write(MessageType.HANDLE);
        session.Writer.Write(requestid);
        session.Writer.Write(handle);
    }

    private static void ReadDirHandler(Session session, uint requestid)
    {
        var handle = session.Reader.ReadString();

        if (session.FileHandles.TryGetValue(handle, out var path))
        {
            var allfiles = new DirectoryInfo(@"D:\\test").GetFiles();

            // returns SSH_FXP_NAME or SSH_FXP_STATUS with SSH_FX_EOF 
            session.Writer.Write(MessageType.NAME);
            session.Writer.Write(requestid);
            session.Writer.Write((uint)allfiles.Length); // all files at the same time

            foreach (var file in allfiles)
            {
                SendFileInfoWithAttributes(session.Writer, file);
            }
            session.Writer.Write(true); // End of list

            session.FileHandles.Remove(handle);
        }
        else
        {
            SendStatus(session.Writer, requestid, Status.EOF);
        }
    }

    private static void CloseHandler(Session session, uint requestId)
    {
        var handle = session.Reader.ReadString();
        session.FileHandles.Remove(handle);

        SendStatus(session.Writer, requestId, Status.OK);
    }

    private static void SendStatus(SshStreamWriter writer, uint requestId, Status status)
        => SendStatus(writer, requestId, status, GetStatusString(status));

    private static void SendStatus(SshStreamWriter writer, uint requestId, Status status, string errorMessage, string languageTag)
    {
        writer.Write(MessageType.STATUS);
        writer.Write(requestId);
        writer.Write((uint)status); // status code
        writer.Write(errorMessage);
        writer.Write(languageTag);
    }

    private static string GetStatusString(Status status) => status switch
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

    private static string GetHandle() => Guid.NewGuid().ToString("N");

    private static uint GetUnixFileTime(DateTimeOffset time)
    {
        var diff = time - DateTimeOffset.UnixEpoch;
        return (uint)Math.Floor(diff.TotalSeconds);

    }

    private static void SendFileInfoWithAttributes(SshStreamWriter writer, FileInfo fileInfo)
    {
        writer.Write(fileInfo.Name);
        SendAttributes(writer, new Attributes(fileInfo));
    }
    private static void SendAttributes(SshStreamWriter writer, Attributes attributes)
    {
        writer.Write(attributes.Flags);
        writer.Write(attributes.FileSize);
        writer.Write(attributes.Uid); // uid
        writer.Write(attributes.Gid); // gid
        writer.Write(attributes.Permissions); // permissions
        writer.Write(GetUnixFileTime(attributes.ATime)); //atime   
        writer.Write(GetUnixFileTime(attributes.MTime)); //mtime
        writer.Write((uint)0); // extended_count
                               //string   extended_type blank
                               //string   extended_data blank
    }
}


internal static class Dumper
{
    public static string Dump(uint data)
        => Dump(BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(data)));

    public static string Dump(string data)
        => Dump(Encoding.UTF8.GetBytes(data));

    public static string Dump(byte[] data)
        => string.Join(" ", data.Select(b => b.ToString("X2")));
}