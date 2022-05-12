namespace SFTPTest.Requests;

public record InitRequest(uint RequestId, uint ClientVersion) : Request(RequestId);