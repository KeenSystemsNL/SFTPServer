
namespace SFTPTest;

public interface IServer
{
    Task Run(CancellationToken cancellationToken = default);
}