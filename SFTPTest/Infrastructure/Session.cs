using Microsoft.Extensions.Logging;

namespace SFTPTest.Infrastructure;

internal record Session(SshStreamReader Reader, SshStreamWriter Writer, FileHandleCollection FileHandles, ILogger Logger);
