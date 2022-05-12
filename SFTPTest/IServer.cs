
namespace SFTPTest;

public interface IServer
{
    Task RunAsync(Stream @in, Stream @out, CancellationToken cancellationToken);
}