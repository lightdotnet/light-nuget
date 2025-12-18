using Light.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace Light.EntityFrameworkCore.Extensions;

public static class ModelBuilderExtensions
{
    /// <summary>
    /// Default apply query filter by interface 
    /// </summary>
    public static ModelBuilder AppendGlobalQueryFilter<TInterface>(this ModelBuilder modelBuilder, Expression<Func<TInterface, bool>> filter)
    {
        // get a list of entities without a baseType that implement the interface TInterface
        var entities = modelBuilder.Model.GetEntityTypes()
            .Where(e => e.BaseType is null && e.ClrType.GetInterface(typeof(TInterface).Name) is not null)
            .Select(e => e.ClrType);

        foreach (var entity in entities)
        {
            var parameterType = Expression.Parameter(modelBuilder.Entity(entity).Metadata.ClrType);
            var filterBody = ReplacingExpressionVisitor.Replace(filter.Parameters.Single(), parameterType, filter.Body);

            // get the existing query filter
            if (modelBuilder.Entity(entity).Metadata.GetQueryFilter() is { } existingFilter)
            {
                var existingFilterBody = ReplacingExpressionVisitor.Replace(existingFilter.Parameters.Single(), parameterType, existingFilter.Body);

                // combine the existing query filter with the new query filter
                filterBody = Expression.AndAlso(existingFilterBody, filterBody);
            }

            // apply the new query filter
            modelBuilder.Entity(entity).HasQueryFilter(Expression.Lambda(filterBody, parameterType));
        }

        return modelBuilder;
    }

    /// <summary>
    /// Apply query filter by interface when condition is true
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