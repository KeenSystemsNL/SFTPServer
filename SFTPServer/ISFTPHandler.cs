using SFTP.Models;

namespace SFTP;

public interface ISFTPHandler
{
    Task<SFTPExtensions> Init(uint clientVersion, string user, SFTPExtensions extensions, CancellationToken cancellationToken = default);
    Task<SFTPHandle> Open(SFTPPath path, FileMode fileMode, FileAccess fileAccess, SFTPAttributes attributes, CancellationToken cancellationToken = default);
    Task Close(SFTPHandle handle, CancellationToken cancellationToken = default);
    Task<SFTPData> Read(SFTPHandle handle, ulong offset, uint length, CancellationToken cancellationToken = default);
    Task Write(SFTPHandle handle, ulong offset, byte[] data, CancellationToken cancellationToken = default);
    Task<SFTPAttributes> LStat(SFTPPath path, CancellationToken cancellationToken = default);
    Task<SFTPAttributes> FStat(SFTPHandle handle, CancellationToken cancellationToken = default);
    Task SetStat(SFTPPath path, SFTPAttributes attributes, CancellationToken cancellationToken = default);
    Task FSetStat(SFTPHandle handle, SFTPAttributes attributes, CancellationToken cancellationToken = default);
    Task<SFTPHandle> OpenDir(SFTPPath path, CancellationToken cancellationToken = default);
    Task<SFTPNames> ReadDir(SFTPHandle handle, CancellationToken cancellationToken = default);
    Task Remove(SFTPPath path, CancellationToken cancellationToken = default);
    Task MakeDir(SFTPPath path, SFTPAttributes attributes, CancellationToken cancellationToken = default);
    Task RemoveDir(SFTPPath path, CancellationToken cancellationToken = default);
    Task<SFTPPath> RealPath(SFTPPath path, CancellationToken cancellationToken = default);
    Task<SFTPAttributes> Stat(SFTPPath path, CancellationToken cancellationToken = default);
    Task Rename(SFTPPath oldPath, SFTPPath newPath, CancellationToken cancellationToken = default);
    Task Extended(string name, Stream inStream, Stream outStream);
#if NET6_0_OR_GREATER
    Task<SFTPName> ReadLink(SFTPPath path, CancellationToken cancellationToken = default);
    Task SymLink(SFTPPath linkPath, SFTPPath targetPath, CancellationToken cancellationToken = default);
#endif
}
