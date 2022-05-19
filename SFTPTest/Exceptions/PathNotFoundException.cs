namespace SFTPTest.Exceptions;

public class PathNotFoundException : NotFoundException
{
    public string Path { get; init; }

    public PathNotFoundException(string path)
        => Path = path;
}
