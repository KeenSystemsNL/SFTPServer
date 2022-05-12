namespace SFTPTest.Infrastructure;
internal class NonNullableDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    where TKey : notnull
    where TValue : notnull
{ }