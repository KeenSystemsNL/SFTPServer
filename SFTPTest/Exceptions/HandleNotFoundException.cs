using SFTPTest.Enums;

namespace SFTPTest.Exceptions;

public class HandleNotFoundException : SFTPHandlerException
{
    public string Handle { get; init; }

    public HandleNotFoundException(string handle)
        : base(Status.NO_SUCH_FILE) => Handle = handle;
}
