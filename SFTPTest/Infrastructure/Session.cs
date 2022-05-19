using SFTPTest.Infrastructure.IO;

namespace SFTPTest.Infrastructure;

internal record Session(
    SshStreamReader Reader,
    SshStreamWriter Writer,
    FileHandleCollection FileHandles,
    FileStreamCollection FileStreams,
    uint Version,
    string Root
);