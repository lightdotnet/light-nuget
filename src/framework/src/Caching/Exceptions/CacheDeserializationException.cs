namespace Light.Exceptions
{
    public class CacheDeserializationException : InvalidOperationException
    {
        public CacheDeserializationException(string key, Type targetType, Exception innerException)
            : base($"Cache entry '{key}' could not be read as '{targetType}'.", innerException)
        {
            Key = key;
            TargetType = targetType;
        }

        public string Key { get; }

        public Type TargetType { get; }
    }
}
