using System.Collections.ObjectModel;

namespace SFTP.Models;

public class SFTPExtensions : ReadOnlyDictionary<string, string>
{
    public static readonly SFTPExtensions None = new(new Dictionary<string, string>());

    public SFTPExtensions()
        : this(None) { }

    public SFTPExtensions(IDictionary<string, string> extensions)
        : base(extensions) { }
}
