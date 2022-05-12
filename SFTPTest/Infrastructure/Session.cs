namespace SFTPTest.Infrastructure;

internal record Session(SshStreamReader Reader, SshStreamWriter Writer, FileHandleCollection FileHandles);
