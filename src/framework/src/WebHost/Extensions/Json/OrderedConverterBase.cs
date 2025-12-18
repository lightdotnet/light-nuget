using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Light.Extensions.Json;

public abstract class OrderedConverterBase<T> : JsonConverter<T> where T : class
{
    private readonly JsonSerializerOptions _safeOptions;

    public OrderedConverterBase(JsonSerializerOptions options)
    {
        // Clone options, but exclude this factory to avoid recursion
        _safeOptions = new JsonSerializerOptions(options);
        for (int i = _safeOptions.Converters.Count - 1; i >= 0; i--)
        {
            if (_safeOptions.Converters[i] is BaseFirstOrderedConverterFactory)
            {
                _safeOptions.Converters.RemoveAt(i);
            }
        }
    }

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<T>(ref reader, _safeOptions)!;
    }

    public abstract IEnumerable<PropertyInfo> GetPropertyInfos();

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        var orderedProps = GetPropertyInfos();

        writer.WriteStartObject();

        foreach (var prop in orderedProps)
        {
            var propValue = prop.GetValue(value, null);
            var propName = _safeOptions.PropertyNamingPolicy?.ConvertName(prop.Name) ?? prop.Name;

            writer.WritePropertyName(propName);
            JsonSerializer.Serialize(writer, propValue, prop.PropertyType, _safeOptions);
        }

        writer.WriteEndObject();
    }
}
