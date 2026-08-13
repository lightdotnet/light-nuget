namespace Light.Infrastructure
{
    public class CacheOptions
    {
        public string Provider { get; set; } = "memory";

        public string? RedisHost { get; set; }

        public string? RedisPassword { get; set; }
    }
}