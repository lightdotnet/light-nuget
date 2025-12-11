using Light.Contracts;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Light.Extensions.Json;

public class PropertyOrderedConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return
            typeToConvert.IsClass
            && !typeToConvert.IsAbstract
            && typeToConvert.GetProperties()
                .Any(p => p.GetCustomAttribute<PropertyOrderAttribute>() != null);
    }

    public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
    {
        var converterType = typeof(PropertyOrderedConverter<>).MakeGenericType(type);
        return (JsonConverter)Activator.CreateInstance(converterType, options)!;
    }
}

public class PropertyOrderedConverter<T>(JsonSerializerOptions options) : OrderedConverterBase<T>(options)
    where T : class
{
    public override IOrderedEnumerable<PropertyInfo> GetPropertyInfos()
    {
        return typeof(T)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .OrderBy(p =>
            {
                var attr = p.GetCustomAttribute<PropertyOrderAttribute>();
                return attr?.Order ?? 0;
            });
    }
}
