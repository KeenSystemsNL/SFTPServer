namespace SFTP;

public record SFTPServerOptions()
{
    public int MaxMessageSize { get; init; } = 1024 * 1024;
    public string Root { get; init; } = string.Empty;
}
