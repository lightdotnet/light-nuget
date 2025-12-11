using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Light.Extensions.Json;

public class BaseFirstOrderedConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        // fix when return result is IEnumerable or Dictionary
        var isNotIEnumerable = !typeof(System.Collections.IEnumerable).IsAssignableFrom(typeToConvert);

        bool canConvert = typeToConvert.IsClass
            && typeToConvert != typeof(string)
            && isNotIEnumerable;

        return canConvert;
    }

    public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
    {
        var converterType = typeof(BaseFirstOrderedConverter<>).MakeGenericType(type);
        return (JsonConverter)Activator.CreateInstance(converterType, options)!;
    }
}

public class BaseFirstOrderedConverter<T>(JsonSerializerOptions options) : OrderedConverterBase<T>(options)
    where T : class
{
    public override IEnumerable<PropertyInfo> GetPropertyInfos()
    {
        // Gather base-to-derived properties
        var type = typeof(T);
        var types = new Stack<Type>();
        while (type != null && type != typeof(object))
        {
            types.Push(type);
            type = type.BaseType!;
        }

        var allProps = new List<PropertyInfo>();
        while (types.Count > 0)
        {
            var currentType = types.Pop();
            var props = currentType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .OrderBy(p => p.GetCustomAttribute<JsonPropertyOrderAttribute>()?.Order ?? 0);
            allProps.AddRange(props);
        }

        return allProps;
    }
}
