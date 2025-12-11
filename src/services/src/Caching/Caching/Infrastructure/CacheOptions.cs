namespace Light.Caching.Infrastructure
{
    public class CacheOptions
    {
        public string Provider { get; set; }

        public string RedisHost { get; set; }

        public string RedisPassword { get; set; }
    }
}