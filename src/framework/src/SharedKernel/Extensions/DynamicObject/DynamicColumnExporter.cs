using System.Globalization;

namespace Light.Extensions.DynamicObject;

public class DynamicColumnExporter
{
    /// <summary>
    /// Export the readable, non-indexer properties of <paramref name="obj"/> as dynamic columns.
    /// </summary>
    /// <remarks>
    /// Values are formatted with <see cref="CultureInfo.InvariantCulture"/>; <see cref="DateTime"/> and
    /// <see cref="DateTimeOffset"/> use the round-trip ("O") format so <see cref="DynamicMapper"/> can read them back.
    /// </remarks>
    public static List<TEntity> ConvertToDynamicColumns<T, TEntity>(T obj, string objectName)
        where TEntity : DynamicEntity
    {
        var columns = new List<TEntity>();
        var type = typeof(T);
        var props = type.GetProperties()
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0);

        foreach (var prop in props)
        {
            var value = prop.GetValue(obj);

            var column = Activator.CreateInstance<TEntity>();

            column.ObjectName = objectName;
            column.PropName = prop.Name;
            column.PropType = GetColumnType(prop.PropertyType);
            column.PropValue = FormatValue(value);

            columns.Add(column);
        }

        return columns;
    }

    private static string? FormatValue(object? value) => value switch
    {
        null => null,
        DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
        DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString(),
    };

    private static string GetColumnType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type == typeof(string))
            return "string";

        if (type == typeof(int))
            return "int";

        if (type == typeof(long))
            return "long";

        if (type == typeof(bool))
            return "bool";

        if (type == typeof(DateTime))
            return "datetime";

        if (type == typeof(DateTimeOffset))
            return "datetime_offset";

        if (type == typeof(decimal))
            return "decimal";

        if (type == typeof(double))
            return "double";
        // Add other types as needed

        return "string"; // fallback
    }
}
