namespace SFTP;

public interface ISFTPServer
{
    Task Run(CancellationToken cancellationToken = default);
}