using SFTPTest.Enums;

namespace SFTPTest.Exceptions;
public abstract class SFTPHandlerException : Exception
{
    public Status Status { get; init; }

    public SFTPHandlerException(Status status)
        => Status = status;
}
