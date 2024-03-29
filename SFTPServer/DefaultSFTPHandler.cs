﻿using SFTP.Exceptions;
using SFTP.Models;
using System.Diagnostics.CodeAnalysis;

namespace SFTP;

public class DefaultSFTPHandler : ISFTPHandler
{
    private readonly Dictionary<SFTPHandle, SFTPPath> _filehandles = new();
    private readonly Dictionary<SFTPHandle, Stream> _streamhandles = new();
    private readonly SFTPPath _root;

    private static readonly Uri _virtualroot = new("virt://", UriKind.Absolute);

    public DefaultSFTPHandler(SFTPPath root)
        => _root = root ?? throw new ArgumentNullException(nameof(root));

    public virtual Task<SFTPExtensions> Init(uint clientVersion, string user, SFTPExtensions extensions, CancellationToken cancellationToken = default)
        => Task.FromResult(SFTPExtensions.None);

    public virtual Task<SFTPHandle> Open(SFTPPath path, FileMode fileMode, FileAccess fileAccess, SFTPAttributes attributes, CancellationToken cancellationToken = default)
    {
        var handle = CreateHandle();
        _streamhandles.Add(handle, File.Open(GetPhysicalPath(path), fileMode, fileAccess, FileShare.ReadWrite));
        _filehandles.Add(handle, path);
        return Task.FromResult(handle);
    }

    public virtual Task Close(SFTPHandle handle, CancellationToken cancellationToken = default)
    {
        _filehandles.Remove(handle);

        if (TryGetStreamHandle(handle, out var stream))
        {
            stream.Close();
            stream.Dispose();
        }
        _streamhandles.Remove(handle);
        return Task.CompletedTask;
    }

    public virtual async Task<SFTPData> Read(SFTPHandle handle, ulong offset, uint length, CancellationToken cancellationToken = default)
    {
        if (TryGetStreamHandle(handle, out var stream))
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

    public virtual async Task Write(SFTPHandle handle, ulong offset, byte[] data, CancellationToken cancellationToken = default)
    {
        if (TryGetStreamHandle(handle, out var stream))
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

    public virtual Task<SFTPAttributes> LStat(SFTPPath path, CancellationToken cancellationToken = default)
        => TryGetFSObject(path, out var fso)
            ? Task.FromResult(SFTPAttributes.FromFileSystemInfo(fso))
            : throw new PathNotFoundException(path);

    public virtual Task<SFTPAttributes> FStat(SFTPHandle handle, CancellationToken cancellationToken = default)
        => TryGetFileHandle(handle, out var path)
        ? Stat(path, cancellationToken)
        : throw new HandleNotFoundException(handle);

    public virtual Task SetStat(SFTPPath path, SFTPAttributes attributes, CancellationToken cancellationToken = default)
        => DoStat(path, attributes, cancellationToken);

    public virtual Task FSetStat(SFTPHandle handle, SFTPAttributes attributes, CancellationToken cancellationToken = default)
        => TryGetFileHandle(handle, out var path)
            ? SetStat(path, attributes, cancellationToken)
            : throw new HandleNotFoundException(handle);

    public virtual Task<SFTPHandle> OpenDir(SFTPPath path, CancellationToken cancellationToken = default)
    {
        var handle = CreateHandle();
        _filehandles.Add(handle, path);
        return Task.FromResult(handle);
    }

    public virtual Task<IEnumerable<SFTPName>> ReadDir(SFTPHandle handle, CancellationToken cancellationToken = default)
        => TryGetFileHandle(handle, out var path)
            ? Task.FromResult(new DirectoryInfo(GetPhysicalPath(path)).GetFileSystemInfos().Select(fso => SFTPName.FromFileSystemInfo(fso)))
            : throw new HandleNotFoundException(handle);

    public virtual Task Remove(SFTPPath path, CancellationToken cancellationToken = default)
    {
        if (TryGetFSObject(path, out var fsObject) && fsObject is FileInfo)
        {
            File.Delete(fsObject.FullName);
            return Task.CompletedTask;
        }
        throw new PathNotFoundException(path);
    }

    public virtual Task MakeDir(SFTPPath path, SFTPAttributes attributes, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(GetPhysicalPath(path));
        return Task.CompletedTask;
    }

    public virtual Task RemoveDir(SFTPPath path, CancellationToken cancellationToken = default)
    {
        if (TryGetFSObject(path, out var fsObject) && fsObject is DirectoryInfo)
        {
            Directory.Delete(fsObject.FullName);
            return Task.CompletedTask;
        }
        throw new PathNotFoundException(path);
    }

    public virtual Task<SFTPPath> RealPath(SFTPPath path, CancellationToken cancellationToken = default)
        => Task.FromResult(new SFTPPath(GetVirtualPath(path)));

    public virtual Task<SFTPAttributes> Stat(SFTPPath path, CancellationToken cancellationToken = default)
        => LStat(path, cancellationToken);

    public virtual Task Rename(SFTPPath oldPath, SFTPPath newPath, CancellationToken cancellationToken = default)
    {
        if (TryGetFSObject(oldPath, out var fsOldObject) && fsOldObject is FileInfo)
        {
            File.Move(fsOldObject.FullName, GetPhysicalPath(newPath));
            return Task.CompletedTask;
        }
        throw new PathNotFoundException(oldPath);
    }

#if NET6_0_OR_GREATER
    public virtual Task<SFTPName> ReadLink(SFTPPath path, CancellationToken cancellationToken = default)
    {
        if (TryGetFSObject(path, out var fsObject) && fsObject.LinkTarget != null)
        {
            return Task.FromResult(SFTPName.FromString(fsObject.LinkTarget));
        }
        throw new PathNotFoundException(path);
    }

    public virtual Task SymLink(SFTPPath linkPath, SFTPPath targetPath, CancellationToken cancellationToken = default)
    {
        var link = GetPhysicalPath(linkPath);
        if (TryGetFSObject(targetPath, out var fsObject))
        {
            switch (fsObject)
            {
                case FileInfo:
                    File.CreateSymbolicLink(link, fsObject.FullName);
                    break;
                case DirectoryInfo:
                    Directory.CreateSymbolicLink(link, fsObject.FullName);
                    break;
            }
            return Task.CompletedTask;
        }
        throw new PathNotFoundException(targetPath);
    }
#endif

    public virtual Task Extended(string name, Stream inStream, Stream outStream)
        => throw new NotImplementedException();

    public virtual string GetPhysicalPath(SFTPPath path)
        => Path.Join(_root.Path, GetVirtualPath(path));

    public virtual string GetVirtualPath(SFTPPath path)
        => new Uri(_virtualroot, path.Path).LocalPath;

    private Task DoStat(SFTPPath path, SFTPAttributes attributes, CancellationToken cancellationToken = default)
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

    private bool TryGetFSObject(SFTPPath path, [NotNullWhen(true)] out FileSystemInfo? fileSystemObject)
    {
        var resolved = GetPhysicalPath(path);
        if (Directory.Exists(resolved))
        {
            fileSystemObject = new DirectoryInfo(resolved);
            return true;
        }
        if (File.Exists(resolved))
        {
            fileSystemObject = new FileInfo(resolved);
            return true;
        }
        fileSystemObject = null;
        return false;
    }

    private static SFTPHandle CreateHandle()
        => new(Guid.NewGuid().ToString("N"));

    protected bool TryGetFileHandle(SFTPHandle key, [NotNullWhen(true)] out SFTPPath? path)
        => _filehandles.TryGetValue(key, out path);
    protected bool TryGetStreamHandle(SFTPHandle key, [NotNullWhen(true)] out Stream? stream)
        => _streamhandles.TryGetValue(key, out stream);
}