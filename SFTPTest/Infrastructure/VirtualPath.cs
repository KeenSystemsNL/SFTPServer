namespace SFTPTest.Infrastructure;

public class VirtualPath : FileSystemInfo
{
    public override bool Exists => true;
    public override string Name { get; }

    public VirtualPath(string name) => Name = name ?? throw new ArgumentNullException(nameof(name));

    public override void Delete() => throw new NotImplementedException();
}