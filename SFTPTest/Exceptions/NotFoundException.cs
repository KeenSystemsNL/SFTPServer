using Microsoft.Extensions.Logging;
using SFTPTest.Enums;

namespace SFTPTest.Exceptions;

public abstract class NotFoundException : SFTPHandlerException
{
    public NotFoundException()
        : base(LogLevel.Debug, Status.NO_SUCH_FILE) { }
}