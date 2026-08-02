namespace Light.AspNetCore.Authorization;

public sealed class PermissionDefinition(
    string name,
    string displayName,
    string? parent = null)
{
    public string Name { get; } = name;

    public string DisplayName { get; } = displayName;

    public string? Parent { get; } = parent;
}
