using SFTP.Enums;

namespace SFTP.Exceptions;

public abstract class NotFoundException : HandlerException
{
    public NotFoundException()
        : base(Status.NoSuchFile) { }
}