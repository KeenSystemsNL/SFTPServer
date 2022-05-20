using SFTPTest.Models;

namespace SFTPTest.Exceptions;

public class HandleNotFoundException : NotFoundException
{
    public SFTPHandle Handle { get; init; }

    public HandleNotFoundException(SFTPHandle handle)
        => Handle = handle;
}
