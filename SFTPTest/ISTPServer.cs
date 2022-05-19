
namespace SFTPTest;

public interface ISTPServer
{
    Task Run(CancellationToken cancellationToken = default);
}