using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;

namespace Light.Extensions.DynamicObject;

public class DynamicMapper
{
    private static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> PropertyCache = new();

    /// <summary>
    /// Map dynamic columns (as produced by <see cref="DynamicColumnExporter"/>) back to an object.
    /// </summary>
    /// <remarks>
    /// Values are parsed with <see cref="CultureInfo.InvariantCulture"/>; dates accept the round-trip ("O") format
    /// written by <see cref="DynamicColumnExporter"/> and fall back to the current culture for legacy values.
    /// Nullable targets (e.g. <c>int?</c>) are supported. Indexers and read-only properties are skipped.
    /// </remarks>
    public static T MapToObject<T, TEntity>(List<TEntity> columns)
        where T : new()
        where TEntity : DynamicEntity
    {
        var obj = new T();
        var properties = PropertyCache.GetOrAdd(typeof(T), static type =>
            type.GetProperties()
                .Where(p => p.GetIndexParameters().Length == 0)
                .GroupBy(p => p.Name)
                .ToDictionary(g => g.Key, g => g.First()));

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
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type == typeof(string))
            return value;

        if (type == typeof(int))
            return int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);

        if (type == typeof(long))
            return long.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);

        if (type == typeof(bool))
            return bool.Parse(value);

        if (type == typeof(DateTime))
        {
            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dateTime)
                ? dateTime
                : DateTime.Parse(value, CultureInfo.CurrentCulture);
        }

        if (type == typeof(DateTimeOffset))
        {
            return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dateTimeOffset)
                ? dateTimeOffset
                : DateTimeOffset.Parse(value, CultureInfo.CurrentCulture);
        }

        if (type == typeof(double))
            return double.Parse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture);

        if (type == typeof(decimal))
            return decimal.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture);

        if (type == typeof(Guid))
            return Guid.Parse(value);

        if (type.IsEnum)
            return Enum.Parse(type, value);

        // Add more as needed

        return Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
    }
}
