
namespace SFTPTest;

public interface IServer
{
    void Run(Stream @in, Stream @out);
}