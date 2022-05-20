using SFTPTest.Models;

namespace SFTPTest.Exceptions;

public class PathNotFoundException : NotFoundException
{
    public SFTPPath Path { get; init; }

    public PathNotFoundException(SFTPPath path)
        => Path = path;
}
