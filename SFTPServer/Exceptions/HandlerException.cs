using SFTP.Enums;

namespace SFTP.Exceptions;
public abstract class HandlerException : Exception
{
    public Status Status { get; init; }

    public HandlerException(Status status)
        => Status = status;
}
