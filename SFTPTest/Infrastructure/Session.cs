using Microsoft.Extensions.Logging;
using SFTPTest.Infrastructure.IO;

namespace SFTPTest.Infrastructure;

internal record Session(SshStreamReader Reader, SshStreamWriter Writer, FileHandleCollection FileHandles, string Root, ILogger Logger);
