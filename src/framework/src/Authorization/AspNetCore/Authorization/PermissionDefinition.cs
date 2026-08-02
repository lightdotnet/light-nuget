namespace Light.AspNetCore.Authorization;

public sealed class PermissionDefinition(
    string name,
    string? displayName = null,
    string? parent = null)
{
    public string Name { get; } = name;

    public string DisplayName { get; } = displayName ?? name;

    public string? Parent { get; } = parent;
}
