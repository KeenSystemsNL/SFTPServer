
namespace SFTPTest;

public interface IServer
{
    Task Run(Stream @in, Stream @out, CancellationToken cancellationToken = default);
}