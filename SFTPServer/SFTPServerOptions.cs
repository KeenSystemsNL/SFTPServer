namespace SFTP;

public record SFTPServerOptions()
{
    public int MaxMessageSize { get; init; }
    public string Root { get; init; } = string.Empty;
}
