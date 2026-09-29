using Light.Extensions.DependencyInjection;
using Light.Extensions.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace WebHost.Tests;

public class JsonOrderingTests
{
    public class BaseDto
    {
        public int Id { get; set; }

        [JsonPropertyName("created_by")]
        public string? CreatedBy { get; set; }
    }

    public class DerivedDto : BaseDto
    {
        public string? Name { get; set; }

        [JsonIgnore]
        public string? Secret { get; set; }

        [JsonPropertyOrder(-1)]
        public string? Code { get; set; }

        public string this[int index] => index.ToString();

        public string WriteOnly { set { } }
    }

    private static readonly DerivedDto Sample = new()
    {
        Id = 1,
        CreatedBy = "me",
        Name = "n",
        Secret = "s",
        Code = "c",
    };

    [Test]
    public void BaseFirstModifier_OrdersBaseFirst_AndHonorsAttributes()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver().WithAddedModifier(JsonPropertyOrderModifiers.BaseFirst),
        };

        var json = JsonSerializer.Serialize(Sample, options);

        Assert.That(json, Is.EqualTo("""{"id":1,"created_by":"me","code":"c","name":"n"}"""));
    }

    [Test]
    public void AddDefaultJsonOptions_UsesBaseFirstResolver()
    {
        var services = new ServiceCollection();
        services.AddControllers().AddDefaultJsonOptions();

        var options = services.BuildServiceProvider()
            .GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;

        Assert.That(options.Converters.Any(c => c.GetType().Name.Contains("Ordered")), Is.False);

        var json = JsonSerializer.Serialize(Sample, options);

        Assert.That(json, Is.EqualTo("""{"id":1,"created_by":"me","code":"c","name":"n"}"""));
    }

    [Test]
    public void AddDefaultJsonOptions_HonorsDefaultIgnoreCondition()
    {
        var services = new ServiceCollection();
        services.AddControllers()
            .AddDefaultJsonOptions()
            .AddJsonOptions(o => o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);

        var options = services.BuildServiceProvider()
            .GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;

        var json = JsonSerializer.Serialize(new DerivedDto { Id = 2 }, options);

        Assert.That(json, Is.EqualTo("""{"id":2}"""));
    }

    [Test]
    public void PropertyOrderModifier_OrdersByPropertyOrderAttribute()
    {
        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver().WithAddedModifier(JsonPropertyOrderModifiers.PropertyOrder),
        };

        var json = JsonSerializer.Serialize(new PropertyOrdered { A = 1, B = 2 }, options);

        Assert.That(json, Is.EqualTo("""{"B":2,"A":1}"""));
    }

    public class PropertyOrdered
    {
        [PropertyOrder(2)]
        public int A { get; set; }

        [PropertyOrder(1)]
        public int B { get; set; }
    }

#pragma warning disable CS0618 // legacy converters are obsolete but must keep working
    [Test]
    public void LegacyBaseFirstConverter_SkipsIgnoredIndexersAndWriteOnly_AndHonorsName()
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        options.Converters.Add(new BaseFirstOrderedConverterFactory());

        var json = JsonSerializer.Serialize(Sample, options);

        Assert.That(json, Is.EqualTo("""{"id":1,"created_by":"me","code":"c","name":"n"}"""));
    }
#pragma warning restore CS0618
}
