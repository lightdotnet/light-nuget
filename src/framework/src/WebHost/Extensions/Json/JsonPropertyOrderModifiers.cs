using System.Reflection;
using System.Text.Json.Serialization.Metadata;

namespace Light.Extensions.Json;

/// <summary>
/// <see cref="DefaultJsonTypeInfoResolver"/> modifiers that re-order JSON properties.
/// Unlike the legacy ordered converters, these only change <see cref="JsonPropertyInfo.Order"/>,
/// so every other System.Text.Json feature ([JsonIgnore], [JsonPropertyName], naming policy,
/// DefaultIgnoreCondition, custom converters, ...) keeps working.
/// </summary>
/// <example>
/// <code>
/// options.TypeInfoResolver = (options.TypeInfoResolver ?? new DefaultJsonTypeInfoResolver())
///     .WithAddedModifier(JsonPropertyOrderModifiers.BaseFirst);
/// </code>
/// </example>
public static class JsonPropertyOrderModifiers
{
    /// <summary>
    /// Orders properties base-class-first (inheritance depth of the declaring type),
    /// then by <c>[JsonPropertyOrder]</c>, then by declaration order.
    /// </summary>
    public static void BaseFirst(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object || typeInfo.Properties.Count < 2)
            return;

        Reorder(typeInfo, p => GetInheritanceDepth(GetDeclaringType(p)));
    }

    /// <summary>
    /// Orders properties by <see cref="PropertyOrderAttribute"/> (missing = 0), then by
    /// <c>[JsonPropertyOrder]</c>, then by declaration order.
    /// </summary>
    public static void PropertyOrder(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object || typeInfo.Properties.Count < 2)
            return;

        Reorder(typeInfo, p => (p.AttributeProvider as MemberInfo)?.GetCustomAttribute<PropertyOrderAttribute>()?.Order ?? 0);
    }

    private static void Reorder(JsonTypeInfo typeInfo, Func<JsonPropertyInfo, int> primaryKey)
    {
        // STJ sorts by Order when the contract is finalized; assign a dense sequence so the
        // resulting order is fully deterministic (the existing Order holds [JsonPropertyOrder]).
        var ordered = typeInfo.Properties
            .Select((p, index) => (Property: p, Primary: primaryKey(p), p.Order, Index: index))
            .OrderBy(x => x.Primary)
            .ThenBy(x => x.Order)
            .ThenBy(x => x.Index)
            .ToList();

        for (var i = 0; i < ordered.Count; i++)
        {
            ordered[i].Property.Order = i;
        }
    }

    private static Type? GetDeclaringType(JsonPropertyInfo property) => property.AttributeProvider switch
    {
        // use the original (base) declaration for overridden virtual properties
        PropertyInfo pi => (pi.GetMethod ?? pi.SetMethod)?.GetBaseDefinition().DeclaringType ?? pi.DeclaringType,
        MemberInfo mi => mi.DeclaringType,
        _ => null, // e.g. properties added by other modifiers: keep them last
    };

    private static int GetInheritanceDepth(Type? type)
    {
        if (type is null)
            return int.MaxValue;

        var depth = 0;
        for (var t = type.BaseType; t is not null; t = t.BaseType)
            depth++;

        return depth;
    }
}
