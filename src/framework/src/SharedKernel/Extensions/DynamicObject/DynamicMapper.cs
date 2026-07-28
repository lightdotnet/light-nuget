using System.Collections.Concurrent;
using System.Reflection;

namespace Light.Extensions.DynamicObject;

public class DynamicMapper
{
    private static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> PropertyCache = new();

    public static T MapToObject<T, TEntity>(List<TEntity> columns)
        where T : new()
        where TEntity : DynamicEntity
    {
        var obj = new T();
        var properties = PropertyCache.GetOrAdd(typeof(T), static type =>
            type.GetProperties().ToDictionary(p => p.Name, p => p));

        foreach (var col in columns)
        {
            if (col.PropValue == null || !properties.TryGetValue(col.PropName, out var prop) || !prop.CanWrite)
            {
                continue;
            }

            var value = ConvertToType(col.PropValue, prop.PropertyType);
            prop.SetValue(obj, value);
        }

        return obj;
    }

    private static object ConvertToType(string value, Type type)
    {
        if (type == typeof(string))
            return value;

        if (type == typeof(int))
            return int.Parse(value);

        if (type == typeof(long))
            return long.Parse(value);

        if (type == typeof(bool))
            return bool.Parse(value);

        if (type == typeof(DateTime) || type == typeof(DateTime?))
            return DateTime.Parse(value);

        if (type == typeof(DateTimeOffset) || type == typeof(DateTimeOffset?))
            return DateTimeOffset.Parse(value);

        if (type == typeof(double))
            return double.Parse(value);

        if (type == typeof(decimal))
            return decimal.Parse(value);

        // Add more as needed

        return Convert.ChangeType(value, type);
    }
}
