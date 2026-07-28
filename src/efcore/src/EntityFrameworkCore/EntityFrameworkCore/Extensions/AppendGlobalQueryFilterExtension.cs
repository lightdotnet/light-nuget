using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace Light.EntityFrameworkCore.Extensions;

public static class ModelBuilderExtensions
{
    /// <summary>
    /// Applies a global query filter to every entity type in the model that implements <typeparamref name="TInterface"/>.
    /// </summary>
    /// <remarks>
    /// The filter is applied once, at the point in the hierarchy where <typeparamref name="TInterface"/> is first
    /// implemented (the root entity type if it implements the interface directly, or the first derived type that
    /// introduces it in a TPH/TPT/TPC hierarchy). EF Core automatically propagates a base type's query filter to its
    /// derived types and combines it (AND) with any filter declared on the derived type itself, so there is no need
    /// to re-apply the same filter at every level of the hierarchy.
    /// Each call registers the filter under a stable, per-<typeparamref name="TInterface"/> filter key
    /// (<c>$"Global_{typeof(TInterface).FullName}"</c>). EF Core 10 combines all filters declared on an entity type —
    /// the default (unnamed) filter and every named filter — with AND when building the query, so filters registered
    /// for different interfaces (e.g. <c>ISoftDelete</c> and <c>ITenantScoped</c>) on the same entity compose
    /// automatically without any manual expression re-writing here.
    /// Calling this method more than once for the *same* <typeparamref name="TInterface"/> replaces the previously
    /// registered filter for that interface (same key) rather than AND-ing the two together — this mirrors
    /// <c>HasQueryFilter</c>'s own "last call wins per key" semantics and matches the documented single-call-per-interface
    /// usage (see README).
    /// </remarks>
    public static ModelBuilder AppendGlobalQueryFilter<TInterface>(this ModelBuilder modelBuilder, Expression<Func<TInterface, bool>> filter)
    {
        var interfaceType = typeof(TInterface);
        var filterKey = $"Global_{interfaceType.FullName}";

        // Entity types that implement TInterface, taken only at the point in the hierarchy where the interface is
        // first introduced (root type, or the first derived type in TPH/TPT/TPC to implement it). This avoids
        // redundantly re-declaring the same filter on every further-derived type — EF Core propagates it automatically.
        var entities = modelBuilder.Model.GetEntityTypes()
            .Where(e => interfaceType.IsAssignableFrom(e.ClrType)
                     && (e.BaseType is null || !interfaceType.IsAssignableFrom(e.BaseType.ClrType)))
            .Select(e => e.ClrType);

        foreach (var entity in entities)
        {
            var entityBuilder = modelBuilder.Entity(entity);
            var clrType = entityBuilder.Metadata.ClrType;
            var parameter = Expression.Parameter(clrType);
            var filterBody = ReplacingExpressionVisitor.Replace(filter.Parameters.Single(), parameter, filter.Body);

            entityBuilder.HasQueryFilter(filterKey, Expression.Lambda(filterBody, parameter));
        }

        return modelBuilder;
    }

    /// <summary>
    /// Applies <see cref="AppendGlobalQueryFilter{TInterface}"/> only when <paramref name="condition"/> is true.
    /// </summary>
    public static ModelBuilder AppendGlobalQueryFilterIf<TInterface>(this ModelBuilder modelBuilder, bool condition, Expression<Func<TInterface, bool>> filter)
    {
        if (condition)
        {
            modelBuilder.AppendGlobalQueryFilter(filter);
        }

        return modelBuilder;
    }
}
