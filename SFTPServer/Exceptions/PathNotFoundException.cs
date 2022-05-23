using SFTP.Models;

namespace SFTP.Exceptions;

public class PathNotFoundException : NotFoundException
{
    public SFTPPath Path { get; init; }

    public PathNotFoundException(SFTPPath path)
        => Path = path;
}