namespace EntityFrameworkCore.Tests;

public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
}

public interface ITenantScoped
{
    string? TenantId { get; set; }
}

public class FilterBaseItem : ISoftDeletable
{
    public int Id { get; set; }
    public bool IsDeleted { get; set; }
}

// Introduces no new filtered interface — used to verify the base type's filter
// propagates to derived-type queries without AppendGlobalQueryFilter re-declaring it.
public class FilterDerivedItem : FilterBaseItem
{
    public string? Name { get; set; }
}

public class FilterTenantItem : ISoftDeletable, ITenantScoped
{
    public int Id { get; set; }
    public bool IsDeleted { get; set; }
    public string? TenantId { get; set; }
}
