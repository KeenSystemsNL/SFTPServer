using Microsoft.Extensions.Logging;
using SFTP.Enums;

namespace SFTP.Exceptions;

public abstract class NotFoundException : HandlerException
{
    public NotFoundException()
        : base(LogLevel.Debug, Status.NO_SUCH_FILE) { }
}