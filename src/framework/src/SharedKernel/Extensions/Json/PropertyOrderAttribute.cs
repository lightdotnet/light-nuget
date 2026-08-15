namespace Light.Extensions.Json;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public sealed class PropertyOrderAttribute(int order) : Attribute
{
    public int Order { get; } = order;
}
