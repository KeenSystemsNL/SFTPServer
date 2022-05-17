namespace SFTPTest.Infrastructure;

internal delegate Task MessageHandler(Session session, uint requestid, CancellationToken cancellationToken = default);