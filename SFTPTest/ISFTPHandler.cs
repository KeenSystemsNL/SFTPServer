using SFTPTest.Models;

namespace SFTPTest;

public interface ISFTPHandler
{
    string GetPath(string root, string path);
    Task<string> RealPath(string path, CancellationToken cancellationToken = default);
    Task<SFTPAttributes> Stat(string path, CancellationToken cancellationToken = default);
    Task<SFTPAttributes> LStat(string path, CancellationToken cancellationToken = default);
    Task<SFTPAttributes> FStat(string handle, CancellationToken cancellationToken = default);
    Task SetStat(string path, SFTPAttributes attributes, CancellationToken cancellationToken = default);
    Task FSetStat(string handle, SFTPAttributes attributes, CancellationToken cancellationToken = default);
    Task<string> OpenDir(string path, CancellationToken cancellationToken = default);
    Task<string> Open(string filename, FileMode fileMode, FileAccess fileAccess, SFTPAttributes attributes, CancellationToken cancellationToken = default);
    Task<SFTPData> Read(string handle, ulong offset, uint length, CancellationToken cancellationToken = default);
    Task Write(string handle, ulong offset, byte[] data, CancellationToken cancellationToken = default);
    Task<SFTPNames> ReadDir(string handle, CancellationToken cancellationToken = default);
    Task Close(string handle, CancellationToken cancellationToken = default);
    Task Remove(string path, CancellationToken cancellationToken = default);
    Task Rename(string oldpath, string newpath, CancellationToken cancellationToken = default);
    Task MakeDir(string path, SFTPAttributes attributes, CancellationToken cancellationToken = default);
    Task RemoveDir(string path, CancellationToken cancellationToken = default);
}
