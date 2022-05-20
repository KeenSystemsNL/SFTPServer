using SFTPTest.Exceptions;
using SFTPTest.Models;
using System.Diagnostics.CodeAnalysis;

namespace SFTPTest;

public class DefaultSFTPHandler : ISFTPHandler
{
    private readonly Dictionary<SFTPHandle, SFTPPath> _filehandles = new();
    private readonly Dictionary<SFTPHandle, Stream> _streamhandles = new();

    public SFTPPath GetPath(SFTPPath root, SFTPPath path)
    {
        var result = Path.GetFullPath(Path.Combine(root.Path, path.Path.TrimStart('/'))).Replace('/', '\\');
        return new SFTPPath(result.StartsWith(root.Path) ? result : root.Path);
    }

    public Task<SFTPHandle> Open(SFTPPath path, FileMode fileMode, FileAccess fileAccess, SFTPAttributes attributes, CancellationToken cancellationToken = default)
    {
        var handle = GetHandle();
        _streamhandles.Add(handle, File.Open(path.Path, fileMode, fileAccess, FileShare.ReadWrite));
        _filehandles.Add(handle, path);
        return Task.FromResult(handle);
    }

    public Task Close(SFTPHandle handle, CancellationToken cancellationToken = default)
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

    public async Task<SFTPData> Read(SFTPHandle handle, ulong offset, uint length, CancellationToken cancellationToken = default)
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

    public async Task Write(SFTPHandle handle, ulong offset, byte[] data, CancellationToken cancellationToken = default)
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

    public Task<SFTPAttributes> LStat(SFTPPath path, CancellationToken cancellationToken = default)
    {
        if (TryGetFSObject(path, out var fso))
        {
            return Task.FromResult(SFTPAttributes.FromFileSystemInfo(fso));
        }
        throw new PathNotFoundException(path);
    }

    public Task<SFTPAttributes> FStat(SFTPHandle handle, CancellationToken cancellationToken = default)
    {
        if (_filehandles.TryGetValue(handle, out var path))
        {
            return Stat(path, cancellationToken);
        }
        throw new HandleNotFoundException(handle);
    }

    public Task SetStat(SFTPPath path, SFTPAttributes attributes, CancellationToken cancellationToken = default)
        => DoStat(path, attributes, cancellationToken);

    public Task FSetStat(SFTPHandle handle, SFTPAttributes attributes, CancellationToken cancellationToken = default)
    {
        if (_filehandles.TryGetValue(handle, out var path))
        {
            return SetStat(path, attributes, cancellationToken);
        }
        throw new HandleNotFoundException(handle);
    }

    public Task<SFTPHandle> OpenDir(SFTPPath path, CancellationToken cancellationToken = default)
    {
        var handle = GetHandle();
        _filehandles.Add(handle, path);
        return Task.FromResult(handle);
    }

    public Task<SFTPNames> ReadDir(SFTPHandle handle, CancellationToken cancellationToken = default)
    {
        if (_filehandles.TryGetValue(handle, out var path))
        {
            _filehandles.Remove(handle);
            return Task.FromResult(new SFTPNames(new DirectoryInfo(path.Path).GetFileSystemInfos().Select(fso => SFTPName.FromFileSystemInfo(fso)).OrderBy(f => f.Name)));
        }
        return Task.FromResult(SFTPNames.EOF);
    }

    public Task Remove(SFTPPath path, CancellationToken cancellationToken = default)
    {
        if (TryGetFSObject(path, out var fsObject) && fsObject is FileInfo)
        {
            File.Delete(fsObject.FullName);
            return Task.CompletedTask;
        }
        throw new PathNotFoundException(path);
    }

    public Task MakeDir(SFTPPath path, SFTPAttributes attributes, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(path.Path);
        return Task.CompletedTask;
    }

    public Task RemoveDir(SFTPPath path, CancellationToken cancellationToken = default)
    {
        if (TryGetFSObject(path, out var fsObject) && fsObject is DirectoryInfo)
        {
            Directory.Delete(path.Path);
            return Task.CompletedTask;
        }
        throw new PathNotFoundException(path);
    }

    public Task<SFTPPath> RealPath(SFTPPath path, CancellationToken cancellationToken = default)
        => Task.FromResult(path);

    public Task<SFTPAttributes> Stat(SFTPPath path, CancellationToken cancellationToken = default)
        => LStat(path, cancellationToken);

    public Task Rename(SFTPPath oldPath, SFTPPath newPath, CancellationToken cancellationToken = default)
    {
        if (TryGetFSObject(oldPath, out var fsOldObject) && fsOldObject is FileInfo)
        {
            File.Move(fsOldObject.FullName, newPath.Path);
            return Task.CompletedTask;
        }
        throw new PathNotFoundException(oldPath);
    }

    public Task<SFTPName> ReadLink(SFTPPath path, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task SymLink(SFTPPath linkPath, SFTPPath targetPath, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();


    private static Task DoStat(SFTPPath path, SFTPAttributes attributes, CancellationToken cancellationToken = default)
    {
        if (TryGetFSObject(path, out var fsoObject))
        {
            if (attributes.LastAccessedTime != DateTimeOffset.MinValue)
            {
                fsoObject.LastAccessTimeUtc = attributes.LastAccessedTime.UtcDateTime;
            }
            if (attributes.LastModifiedTime != DateTimeOffset.MinValue)
            {
                fsoObject.LastWriteTimeUtc = attributes.LastModifiedTime.UtcDateTime;
            }
            // TODO: Read/Write/Execute... etc.
        }

        return Task.CompletedTask;
    }

    private static bool TryGetFSObject(SFTPPath path, [NotNullWhen(true)] out FileSystemInfo? fileSystemObject)
    {
        if (Directory.Exists(path.Path))
        {
            fileSystemObject = new DirectoryInfo(path.Path);
            return true;
        }
        if (File.Exists(path.Path))
        {
            fileSystemObject = new FileInfo(path.Path);
            return true;
        }
        fileSystemObject = null;
        return false;
    }

    private static SFTPHandle GetHandle()
        => new(Guid.NewGuid().ToString("N"));
}
