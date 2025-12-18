using System.Text.Json;

namespace Light.Caching.Infrastructure
{
    internal static class CacheDataExtensions
    {
        internal static JsonSerializerOptions DefaultJsonOptions
            => new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };

        internal static T ReadFromJson<T>(this string json)
            => string.IsNullOrEmpty(json)
            ? default
            : JsonSerializer.Deserialize<T>(json, DefaultJsonOptions);

        internal static string JsonSerialize<T>(this T data)
            => JsonSerializer.Serialize(data, DefaultJsonOptions);
    }
}
