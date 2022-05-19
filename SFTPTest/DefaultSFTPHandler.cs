using SFTPTest.Exceptions;
using SFTPTest.Models;
using System.Diagnostics.CodeAnalysis;

namespace SFTPTest;

public class DefaultSFTPHandler : ISFTPHandler
{
    private readonly Dictionary<string, string> _filehandles = new();
    private readonly Dictionary<string, Stream> _streamhandles = new();

    public string GetPath(string root, string path)
    {
        var result = Path.GetFullPath(Path.Combine(root, path.TrimStart('/'))).Replace('/', '\\');
        return result.StartsWith(root) ? result : root;
    }

    public Task Close(string handle, CancellationToken cancellationToken = default)
    {
        _filehandles.Remove(handle);

        if (_streamhandles.TryGetValue(handle, out var stream))
        {
            stream.Close();
            stream.Dispose();
        }
        _streamhandles.Remove(handle);
        return Task.CompletedTask;
    }

    public Task FSetStat(string handle, SFTPAttributes attributes, CancellationToken cancellationToken = default)
    {
        if (_filehandles.TryGetValue(handle, out var path))
        {
            return SetStat(path, attributes, cancellationToken);
        }
        throw new HandleNotFoundException(handle);
    }

    public Task<SFTPAttributes> FStat(string handle, CancellationToken cancellationToken = default)
    {
        if (_filehandles.TryGetValue(handle, out var path))
        {
            return Stat(path, cancellationToken);
        }
        throw new HandleNotFoundException(handle);
    }

    public Task<SFTPAttributes> LStat(string path, CancellationToken cancellationToken = default)
    {
        if (TryGetFSObject(path, out var fso))
        {
            return Task.FromResult(new SFTPAttributes(fso));
        }
        throw new PathNotFoundException(path);
    }

    public Task MakeDir(string path, SFTPAttributes attributes, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(path);
        return Task.CompletedTask;
    }

    public Task<string> Open(string filename, FileMode fileMode, FileAccess fileAccess, SFTPAttributes attributes, CancellationToken cancellationToken = default)
    {
        var handle = GetHandle();
        _streamhandles.Add(handle, File.Open(filename, fileMode, fileAccess, FileShare.ReadWrite));
        _filehandles.Add(handle, filename);
        return Task.FromResult(handle);
    }

    public Task<string> OpenDir(string path, CancellationToken cancellationToken = default)
    {
        var handle = GetHandle();
        _filehandles.Add(handle, path);
        return Task.FromResult(handle);
    }

    public async Task<SFTPData> Read(string handle, ulong offset, uint length, CancellationToken cancellationToken = default)
    {
        if (_streamhandles.TryGetValue(handle, out var stream))
        {
            if (offset < (ulong)stream.Length)
            {
                stream.Seek((long)offset, SeekOrigin.Begin);
                var buff = new byte[length];
                var bytesread = await stream.ReadAsync(buff.AsMemory(0, (int)length), cancellationToken).ConfigureAwait(false);

                return new SFTPData(buff[..bytesread]);
            }
            else
            {
                return SFTPData.EOF;
            }
        }
        throw new HandleNotFoundException(handle);
    }

    public Task<SFTPNames> ReadDir(string handle, CancellationToken cancellationToken = default)
    {
        if (_filehandles.TryGetValue(handle, out var path))
        {
            _filehandles.Remove(handle);
            return Task.FromResult(new SFTPNames(new DirectoryInfo(path).GetFileSystemInfos().OrderBy(f => f.Name)));
        }
        return Task.FromResult(SFTPNames.EOF);
    }

    public Task<string> RealPath(string path, CancellationToken cancellationToken = default)
        => Task.FromResult(path);

    public Task Remove(string path, CancellationToken cancellationToken = default)
    {
        if (TryGetFSObject(path, out var fsObject) && fsObject is FileInfo)
        {
            File.Delete(fsObject.FullName);
            return Task.CompletedTask;
        }
        throw new PathNotFoundException(path);
    }

    public Task RemoveDir(string path, CancellationToken cancellationToken = default)
    {
        if (TryGetFSObject(path, out var fsObject) && fsObject is DirectoryInfo)
        {
            Directory.Delete(path);
            return Task.CompletedTask;
        }
        throw new PathNotFoundException(path);
    }

    public Task Rename(string oldpath, string newpath, CancellationToken cancellationToken = default)
    {
        if (TryGetFSObject(oldpath, out var fsOldObject) && fsOldObject is FileInfo)
        {
            File.Move(fsOldObject.FullName, newpath);
            return Task.CompletedTask;
        }
        throw new PathNotFoundException(oldpath);
    }

    public Task SetStat(string path, SFTPAttributes attributes, CancellationToken cancellationToken = default)
        => DoStat(path, attributes, cancellationToken);

    public Task<SFTPAttributes> Stat(string path, CancellationToken cancellationToken = default)
        => LStat(path, cancellationToken);

    public async Task Write(string handle, ulong offset, byte[] data, CancellationToken cancellationToken = default)
    {
        if (_streamhandles.TryGetValue(handle, out var stream))
        {
            if (stream.Position != (long)offset)
            {
                stream.Seek((long)offset, SeekOrigin.Begin);
            }
            await stream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
            return;
        }
        throw new HandleNotFoundException(handle);
    }

    private static Task DoStat(string path, SFTPAttributes attributes, CancellationToken cancellationToken = default)
    {
        if (attributes.LastModifiedTime != DateTimeOffset.MinValue)
        {
            File.SetLastWriteTimeUtc(path, attributes.LastModifiedTime.UtcDateTime);
        }
        //TODO: implement permissions??
        return Task.CompletedTask;
    }

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
}
