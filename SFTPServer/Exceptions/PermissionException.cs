using SFTP.Enums;

namespace SFTP.Exceptions;

public class PermissionException : HandlerException
{
    public string? Reason { get; init; }

    public PermissionException(string? reason = null)
        : base(Status.PermissionDenied)
        => Reason = reason;
}