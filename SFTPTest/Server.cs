using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Buffers.Binary;
using System.Text;

namespace SFTPTest;

public class Server : IServer
{
    private readonly ServerOptions _options;
    private readonly ILogger<Server> _logger;

    public Server(IOptions<ServerOptions> options, ILogger<Server> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task RunAsync(Stream @in, Stream @out, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Running...");
        var reader = new SshStreamReader(@in);
        var writer = new SshStreamWriter(@out, _options.MaxMessageSize);
        uint msglength;
        uint requestid;
        var handles = new Dictionary<string, string>();
        do
        {
            _logger.LogInformation("Waiting for message");
            msglength = reader.ReadUInt32();
            if (msglength > 0)
            {
                _logger.LogInformation("Len [{msglength}]", msglength);

                var msgtype = (MessageType)reader.ReadByte();

                _logger.LogInformation("Received [{msgtype}]", msgtype);

                switch (msgtype)
                {
                    case MessageType.INIT:
                        var sftpclientversion = reader.ReadUInt32();
                        _logger.LogInformation("Client version [{client version}]", Dumper.Dump(sftpclientversion));
                        writer.Write(MessageType.VERSION);
                        writer.Write((uint)3);
                        break;
                    case MessageType.REALPATH:
                        requestid = reader.ReadUInt32();
                        _logger.LogInformation("Request: {requestid}", Dumper.Dump(requestid));

                        var path = reader.ReadString();
                        _logger.LogInformation("Path: {path} [{hexpath}]", path, Dumper.Dump(path));
                        //var controlbyte = reader.ReadByte();
                        //_logger.LogInformation("Control byte: {path}", path);
                        //var composepath = reader.ReadString(Encoding.UTF8);
                        //_logger.LogInformation("Compose path: {composepath}", composepath);

                        writer.Write(MessageType.NAME);
                        writer.Write(requestid);
                        writer.Write((uint)1);
                        // Dummy file for SSH_FXP_REALPATH request
                        writer.Write(path);
                        writer.Write(@"-rwxr-xr-x   1 mjos     staff      348911 Mar 25 14:29 " + path);
                        writer.Write(requestid);
                        writer.Write(uint.MaxValue); // flags
                        writer.Write((ulong)0); // size
                        writer.Write(uint.MaxValue); // uid
                        writer.Write(uint.MaxValue); // gid
                        writer.Write(uint.MaxValue); // permissions
                        writer.Write(GetUnixFileTime(DateTime.Now)); //atime   
                        writer.Write(GetUnixFileTime(DateTime.Now)); //mtime
                        writer.Write((uint)0); // extended_count
                        break;
                    case MessageType.STAT:
                    case MessageType.LSTAT:
                        requestid = reader.ReadUInt32();
                        path = reader.ReadString();

                        writer.Write(MessageType.ATTRS);
                        writer.Write(requestid);

                        writer.Write(uint.MaxValue); // flags
                        writer.Write((ulong)10); // size
                        writer.Write(uint.MaxValue); // uid
                        writer.Write(uint.MaxValue); // gid
                        writer.Write(uint.MaxValue); // permissions
                        writer.Write(GetUnixFileTime(DateTime.Now)); //atime   
                        writer.Write(GetUnixFileTime(DateTime.Now)); //mtime
                        writer.Write((uint)0); // extended_count
                        break;
                    case MessageType.OPENDIR:
                        requestid = reader.ReadUInt32();
                        path = reader.ReadString();

                        var handle = GetHandle();
                        handles.Add(handle, path);

                        writer.Write(MessageType.HANDLE);
                        writer.Write(requestid);
                        writer.Write(handle);
                        break;
                    case MessageType.CLOSE:
                        requestid = reader.ReadUInt32();
                        handle = reader.ReadString();
                        handles.Remove(handle);
                        SendStatus(writer, requestid, Status.OK);
                        break;
                    case MessageType.STATUS:
                        break;
                    case MessageType.READDIR:
                        requestid = reader.ReadUInt32();
                        handle = reader.ReadString();
                        writer.Write((byte)MessageType.STATUS);
                        writer.Write(requestid);
                        writer.Write((uint)Status.EOF); // status code

                        //if (handles.ContainsKey(handle)) // remove after handle is used first time
                        //{
                        //    var relativepath = handles[handle];
                        //    absolutepath = UserRootDirectory + relativepath;

                        //    System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(absolutepath);
                        //    var allfiles = di.GetFiles();

                        //    // returns SSH_FXP_NAME or SSH_FXP_STATUS with SSH_FX_EOF 
                        //    writer.Write((byte)RequestPacketType.SSH_FXP_NAME);
                        //    writer.Write((uint)requestId);
                        //    writer.Write((uint)allfiles.Count()); // all files at the same time

                        //    foreach (var file in allfiles)
                        //    {
                        //        writer.Write(GetFileWithAttributes(file));

                        //    }

                        //    SendPacket(writer.ToByteArray());

                        //    HandleToPathDictionary.Remove(handle); // remove will return EOF next time

                        //}
                        //else
                        //{
                        //    writer.Write((byte)MessageType.STATUS);
                        //    writer.Write((uint)requestid);
                        //    writer.Write((uint)Status.EOF); // status code
                        //}
                        break;
                }

                writer.Flush(_logger);
            }
        } while (!cancellationToken.IsCancellationRequested && msglength > 0);
        _logger.LogInformation("Cancelled: {cancelled}", cancellationToken.IsCancellationRequested);
        _logger.LogInformation("Message length: {msglength}", msglength);
    }

    private void SendStatus(SshStreamWriter writer, uint requestId, Status status)
    {
        writer.Write(MessageType.STATUS);
        writer.Write(requestId);
        writer.Write((uint)status); // status code
    }

    private string GetHandle() => Guid.NewGuid().ToString("N");

    private uint GetUnixFileTime(DateTime time)
    {
        var diff = time.ToUniversalTime() - DateTime.UnixEpoch;
        return (uint)Math.Floor(diff.TotalSeconds);

    }
}

public static class Dumper
{
    public static string Dump(uint data)
        => Dump(BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(data)));

    public static string Dump(string data)
        => Dump(Encoding.UTF8.GetBytes(data));

    public static string Dump(byte[] data)
        => string.Join(" ", data.Select(b => b.ToString("X2")));
}