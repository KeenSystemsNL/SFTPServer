namespace SFTPTest;

public record ServerOptions()
{
    public int MaxMessageSize { get; init; }
    public string Root { get; init; } = string.Empty;
}
