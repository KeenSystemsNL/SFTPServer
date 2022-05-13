﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SFTPTest.Enums;
using SFTPTest.Infrastructure;
using SFTPTest.Infrastructure.IO;
using System.Buffers.Binary;
using System.Text;

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
        { RequestType.OPENDIR, OpenDirHandler },
        { RequestType.READDIR, ReadDirHandler },
        { RequestType.CLOSE, CloseHandler },
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
        var session = new Session(reader, writer, new FileHandleCollection(), _options.Root, _logger);
        uint msglength;
        var initdone = false;
        do
        {
            msglength = reader.ReadUInt32();
            if (msglength > 0)
            {
                // Determine message type
                var msgtype = (RequestType)reader.ReadByte();
                if (!initdone && msgtype == RequestType.INIT)
                {
                    InitHandler(session);
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

    private static void InitHandler(Session session)
    {
        // Get client version
        var clientversion = session.Reader.ReadUInt32();
        // Send version response (v4)
        session.Writer.Write(RequestType.VERSION);
        session.Writer.Write(4);
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
        SendFSInfoWithAttributes(session.Writer, new VirtualPath(path));
    }

    private static void StatHandler(Session session, uint requestid)
        => LStatHandler(session, requestid);

    private static void LStatHandler(Session session, uint requestid)
    {
        var path = session.Reader.ReadString();
        var flags = session.Reader.ReadUInt32();

        session.Logger.LogInformation("Path: {path}, Flags: {flags}", path, Convert.ToString(flags, 2));

        session.Writer.Write(ResponseType.ATTRS);
        session.Writer.Write(requestid);

        SendAttributes(session.Writer, Attributes.Dummy);   //TODO: Return attributes for path
    }

    private static void FStatHandler(Session session, uint requestid)
    {
        var handle = session.Reader.ReadString();
        var flags = session.Reader.ReadUInt32();

        session.Logger.LogInformation("Handle: {handle}, Flags: {flags}", handle, Convert.ToString(flags, 2));

        session.Writer.Write(ResponseType.ATTRS);
        session.Writer.Write(requestid);

        SendAttributes(session.Writer, Attributes.Dummy);   //TODO: Return attributes for handle
    }

    private static void OpenDirHandler(Session session, uint requestid)
    {
        var path = session.Reader.ReadString();

        var handle = GetHandle();
        session.FileHandles.Add(handle, GetPath(session.Root, path));

        session.Logger.LogInformation("Path: {path}, Handle: {handle}", path, handle);


        session.Writer.Write(ResponseType.HANDLE);
        session.Writer.Write(requestid);
        session.Writer.Write(handle);
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
                SendFSInfoWithAttributes(session.Writer, file);
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

        session.Logger.LogInformation("Handle: {handle}", handle);

        session.FileHandles.Remove(handle);

        SendStatus(session.Writer, requestId, Status.OK);
    }

    private static void SendStatus(SshStreamWriter writer, uint requestId, Status status)
        => SendStatus(writer, requestId, status, GetStatusString(status), string.Empty);

    private static void SendStatus(SshStreamWriter writer, uint requestId, Status status, string errorMessage, string languageTag)
    {
        writer.Write(ResponseType.STATUS);
        writer.Write(requestId);
        writer.Write(status);
        writer.Write(errorMessage);
        writer.Write(languageTag);
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

    private static string GetHandle() => Guid.NewGuid().ToString("N");

    private static void SendFSInfoWithAttributes(SshStreamWriter writer, FileSystemInfo fileInfo)
    {
        writer.Write(fileInfo.Name);
        SendAttributes(writer, new Attributes(fileInfo));
    }

    private static void SendAttributes(SshStreamWriter writer, Attributes attributes)
    {
        var flags = FileAttributeFlags.SIZE
            | FileAttributeFlags.OWNERGROUP
            | FileAttributeFlags.PERMISSIONS
            | FileAttributeFlags.ACCESSTIME
            | FileAttributeFlags.CREATETIME
            | FileAttributeFlags.MODIFYTIME;
        writer.Write(flags);
        writer.Write(attributes.FileType);
        writer.Write(attributes.FileSize);
        writer.Write(attributes.Uid); // uid
        writer.Write(attributes.Gid); // gid
        writer.Write(attributes.Permissions); // permissions
        writer.Write(attributes.ATime.ToUnixTimeSeconds()); //atime   
        writer.Write(attributes.CTime.ToUnixTimeSeconds()); //ctime   
        writer.Write(attributes.MTime.ToUnixTimeSeconds()); //mtime   
        //writer.Write(0);  //extended type
        //writer.Write(0);  //extended data
    }

    private static string GetPath(string root, string path)
    {
        var result = Path.GetFullPath(Path.Combine(root, path.TrimStart('/'))).Replace('/', '\\');
        return result.StartsWith(root) ? result : root;
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

    public static string DumpASCII(byte[] data)
        => string.Join(" ", data.Select(b => (b >= 32 && b < 127 ? (char)b : '.').ToString().PadLeft(2)));

}