using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Light.Extensions.Json
{
    public abstract class UnixTimeToDateTimeConverter : JsonConverter<DateTime>
    {
        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }

    public class UnixSecondsToDateTimeConverter : UnixTimeToDateTimeConverter
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var seconds = reader.GetInt64();
            return DateTime.UnixEpoch.AddSeconds(seconds);
        }
    }

    public class UnixMilliSecondsToDateTimeConverter : UnixTimeToDateTimeConverter
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var seconds = reader.GetInt64();
            return DateTime.UnixEpoch.AddMilliseconds(seconds);
        }
    }
}
