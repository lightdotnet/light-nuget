namespace Light.Extensions.DynamicObject;

public class DynamicColumnExporter
{
    public static List<TEntity> ConvertToDynamicColumns<T, TEntity>(T obj, string objectName)
        where TEntity : DynamicEntity
    {
        var columns = new List<TEntity>();
        var type = typeof(T);
        var props = type.GetProperties();

        foreach (var prop in props)
        {
            var value = prop.GetValue(obj);

            var column = Activator.CreateInstance<TEntity>();

            column.ObjectName = objectName;
            column.PropName = prop.Name;
            column.PropType = GetColumnType(prop.PropertyType);
            column.PropValue = value?.ToString();

            columns.Add(column);
        }

        return columns;
    }

    private static string GetColumnType(Type type)
    {
        if (type == typeof(string))
            return "string";

        if (type == typeof(int))
            return "int";

        if (type == typeof(long))
            return "long";

        if (type == typeof(bool))
            return "bool";

        if (type == typeof(DateTime) || type == typeof(DateTime?))
            return "datetime";

        if (type == typeof(DateTimeOffset) || type == typeof(DateTimeOffset?))
            return "datetime_offset";

        if (type == typeof(decimal))
            return "decimal";

        if (type == typeof(double))
            return "double";
        // Add other types as needed

        return "string"; // fallback
    }
}
