using SFTPTest.Enums;

namespace SFTPTest.Exceptions;

public class PathNotFoundException : SFTPHandlerException
{
    public string Path { get; init; }

    public PathNotFoundException(string path)
        : base(Status.NO_SUCH_FILE) => Path = path;
}
