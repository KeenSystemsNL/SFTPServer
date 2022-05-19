using Microsoft.Extensions.Logging;
using SFTPTest.Enums;

namespace SFTPTest.Exceptions;
public abstract class SFTPHandlerException : Exception
{
    public LogLevel LogLevel { get; init; }
    public Status Status { get; init; }

    public SFTPHandlerException(LogLevel logLevel, Status status)
    {
        LogLevel = logLevel;
        Status = status;
    }
}
