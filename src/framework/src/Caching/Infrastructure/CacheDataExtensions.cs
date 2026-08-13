using System.Text.Json;

namespace Light.Infrastructure
{
    internal static class CacheDataExtensions
    {
        private static readonly JsonSerializerOptions DefaultJsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        internal static T? ReadFromJson<T>(this string? json)
            => string.IsNullOrEmpty(json)
            ? default
            : JsonSerializer.Deserialize<T>(json, DefaultJsonOptions);

        internal static string JsonSerialize<T>(this T data)
            => JsonSerializer.Serialize(data, DefaultJsonOptions);
    }
}
