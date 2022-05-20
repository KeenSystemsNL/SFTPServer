# SFTPServer

This library (and simple host) implement an SFTP server that implements the [V3 version of the SFTP protocol](https://datatracker.ietf.org/doc/html/draft-ietf-secsh-filexfer-02).

This library is intended to be hosted in an SSH deamon; this can be done by editting the `sshd_config` file (usually in `/etc/ssh/` or, for Windows, in `%PROGRAMDATA%\ssh`) and pointing it to your own host executable.

In `sshd_config` you'll find the `Subsystem` entry which can be pointed to any executable:
```
# override default of no subsystems
#Subsystem	sftp	sftp-server.exe
#Subsystem	sftp	internal-sftp
Subsystem	sftp	/path/to/your/sftphost.exe
```

The way SFTP works is that first an SSH connection is established; once that is done and a request is made for sftp, SSH will launch the configured executable under the connected user's account. All communication is done over `stdin` and `stdout`. The `SFTPServer` class takes 2 [`Streams`](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream) and optionally an `ISFTPHandler` on which the SFTP commands will be invoked. This package comes with a `DefaultSFTPHandler` which provides basic I/O on the hosts's filesystem (based on a rootdirectory), but it should be pretty easy to implement your own `ISFTPHandler` so you can, for example, implement a virtual filesystem.

The `SFTPHost` project in this repository demonstrates how to simply build an executable that hosts an `SFTPServer` instance. A new `SFTPServer` is launched by the SSH service for each connection.

## Implementing an `ISFTPHandler`

Implementing an `ISFTPHandler` should be pretty straightforward, simply implement the `ISFTPHandler` interface. Exceptions you might want to throw should, ideally, be inherited from the `HandlerException`. This library comes with a few built-in exceptions in the `SFTP.Exceptions` namespace. Exceptions that are not inherited from the `HandlerException` will be returned to the client as [`Failure` (`SSH_FX_FAILURE`)](https://datatracker.ietf.org/doc/html/draft-ietf-secsh-filexfer-02#page-20). For an example implementation you can have a look at the `DefaultSFTPHandler`.

## Known issues and limitations

* Note that this library only implements V3; higher versions are not supported. Clients connecting with a higher version will be negotiated down to V3.

* The [`SymLink`](https://datatracker.ietf.org/doc/html/draft-ietf-secsh-filexfer-02#section-6.10) command has been implemented with the `linkpath` and `targetpath` swapped; I may or may not interpret the RFC incorrectly or the clients which were used to test the `SymLink` command (WinSCP, Cyberduck and the 'native' sftp commandline executable) had the arguments swapped.

* The `DefaultSFTPHandler` **DOES NOT** take particular much care of path canonicalization or mitigations against path traversion. When used in an untrusted environment extra care should be taken to ensure safety.

* The `DefaultSFTPHandler` **DOES NOT** make a noteworthy effort to return correct POSIX file permissions, nor does it support setting permissions.

## License

Licensed under MIT license. See [LICENSE](https://github.com/KeenSystemsNL/SFTPServer/raw/master/LICENSE) for details.

### Attributions

[Logo / icon by Alexiuz AS](https://icon-icons.com/icon/sftp/117855) ([Archived](https://web.archive.org/web/20220520155358/https://icon-icons.com/icon/sftp/117855))