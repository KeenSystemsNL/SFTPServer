using Microsoft.Extensions.Logging;
using SFTP.Enums;

namespace SFTP.Exceptions;
public abstract class HandlerException : Exception
{
    public LogLevel LogLevel { get; init; }
    public Status Status { get; init; }

    public HandlerException(LogLevel logLevel, Status status)
    {
        LogLevel = logLevel;
        Status = status;
    }
}
