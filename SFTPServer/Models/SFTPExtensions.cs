using System.Collections.ObjectModel;

namespace SFTP.Models;

public class SFTPExtensions : ReadOnlyDictionary<string, string>
{
    private static readonly IDictionary<string, string> _empty = new Dictionary<string, string>();

    public SFTPExtensions()
        : this(_empty) { }

    public SFTPExtensions(IDictionary<string, string> extensions)
        : base(extensions) { }
}
