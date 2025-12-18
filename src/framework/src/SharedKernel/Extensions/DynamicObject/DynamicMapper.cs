namespace Light.Extensions.DynamicObject;

public class DynamicMapper
{
    public static T MapToObject<T, TEntity>(List<TEntity> columns)
        where T : new()
        where TEntity : DynamicEntity
    {
        var obj = new T();
        var type = typeof(T);

        foreach (var col in columns)
        {
            var prop = type.GetProperty(col.PropName!);
            if (prop != null && prop.CanWrite)
            {
                try
                {
                    if (col.PropValue != null)
                    {
                        object value = ConvertToType(col.PropValue, prop.PropertyType);
                        prop.SetValue(obj, value);
                    }
                }
                catch
                {
                    // Optional: log or handle conversion errors
                }
            }
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
