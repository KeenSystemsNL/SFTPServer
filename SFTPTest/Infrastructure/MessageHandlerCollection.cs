using SFTPTest.Enums;

namespace SFTPTest.Infrastructure;

internal class MessageHandlerCollection : NonNullableDictionary<RequestType, MessageHandler> { }