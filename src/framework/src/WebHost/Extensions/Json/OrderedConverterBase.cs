using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Light.Extensions.Json;

[Obsolete("Use a DefaultJsonTypeInfoResolver modifier from JsonPropertyOrderModifiers instead (e.g. resolver.WithAddedModifier(JsonPropertyOrderModifiers.BaseFirst)).")]
public abstract class OrderedConverterBase<T> : JsonConverter<T> where T : class
{
    private readonly JsonSerializerOptions _safeOptions;
    private WritableProperty[]? _cachedProps;

    private sealed record WritableProperty(PropertyInfo Property, string Name, JsonIgnoreCondition? IgnoreCondition);

    public OrderedConverterBase(JsonSerializerOptions options)
    {
        // Clone options, but exclude the "ordered" converter factories to avoid recursion
        // (a self-referencing/nested property of the same T would otherwise recurse indefinitely)
        _safeOptions = new JsonSerializerOptions(options);
        for (int i = _safeOptions.Converters.Count - 1; i >= 0; i--)
        {
            if (_safeOptions.Converters[i] is BaseFirstOrderedConverterFactory or PropertyOrderedConverterFactory)
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
        // GetPropertyInfos() reflects + orders once; cache the materialized result since STJ
        // creates one converter instance per type and reuses it for every Write() call.
        var props = _cachedProps ??= BuildWritableProperties();

        writer.WriteStartObject();

        foreach (var prop in props)
        {
            var propValue = prop.Property.GetValue(value, null);

            if (ShouldSkip(prop, propValue))
                continue;

            writer.WritePropertyName(prop.Name);
            JsonSerializer.Serialize(writer, propValue, prop.Property.PropertyType, _safeOptions);
        }

        writer.WriteEndObject();
    }

    private WritableProperty[] BuildWritableProperties()
    {
        return GetPropertyInfos()
            // skip indexers and properties without a public getter
            .Where(p => p.GetIndexParameters().Length == 0 && p.GetMethod?.IsPublic == true)
            .Select(p =>
            {
                var ignore = p.GetCustomAttribute<JsonIgnoreAttribute>();
                var name = p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
                    ?? _safeOptions.PropertyNamingPolicy?.ConvertName(p.Name)
                    ?? p.Name;

                return new WritableProperty(p, name, ignore?.Condition);
            })
            .Where(p => p.IgnoreCondition != JsonIgnoreCondition.Always)
            .ToArray();
    }

    private bool ShouldSkip(WritableProperty prop, object? propValue)
    {
        // [JsonIgnore(Condition = ...)] wins over the options-level DefaultIgnoreCondition
        var condition = prop.IgnoreCondition ?? _safeOptions.DefaultIgnoreCondition;

        return condition switch
        {
            JsonIgnoreCondition.WhenWritingNull => propValue is null,
            JsonIgnoreCondition.WhenWritingDefault => propValue is null
                || (prop.Property.PropertyType.IsValueType
                    && propValue.Equals(Activator.CreateInstance(prop.Property.PropertyType))),
            _ => false,
        };
    }
}
