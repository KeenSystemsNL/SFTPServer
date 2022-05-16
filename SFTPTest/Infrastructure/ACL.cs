using SFTPTest.Enums;

namespace SFTPTest.Infrastructure;

public record ACL(ACEType ACEType, ACEFlags ACEFlags, ACEMask ACEMask, string Who);