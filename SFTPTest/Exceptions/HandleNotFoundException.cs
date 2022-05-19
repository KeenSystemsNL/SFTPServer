namespace SFTPTest.Exceptions;

public class HandleNotFoundException : NotFoundException
{
    public string Handle { get; init; }

    public HandleNotFoundException(string handle)
        => Handle = handle;
}
