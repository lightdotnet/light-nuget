using Light.Extensions.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UnitTests.ExtensionsTests
{
    public class JsonTimeModel
    {
        [JsonPropertyName("seconds")]
        [UnixSecondsDateTime]
        public DateTime SecondsAsTime { get; set; }

        [JsonPropertyName("milliseconds")]
        [UnixMilliSecondsDateTime]
        public DateTime MilliSecondsAsTime { get; set; }
    }

    public class JsonUnixTimeTests
    {
        [Fact]
        public void Must_Convert_Correct_Time()
        {
            var time = new DateTimeOffset(new DateTime(2024, 01, 01, 00, 00, 00, DateTimeKind.Utc));

            var body = new
            {
                seconds = time.ToUnixTimeSeconds(),
                milliseconds = time.ToUnixTimeMilliseconds()
            };

            var json = JsonSerializer.Serialize(body);

            var result = JsonSerializer.Deserialize<JsonTimeModel>(json);

            result!.SecondsAsTime.ShouldBe(time.DateTime);
            result.MilliSecondsAsTime.ShouldBe(time.DateTime);
        }
    }
}
