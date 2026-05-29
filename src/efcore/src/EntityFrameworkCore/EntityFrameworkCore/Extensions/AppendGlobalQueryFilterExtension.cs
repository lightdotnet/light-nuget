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
            .Where(e => e.BaseType is null && typeof(TInterface).IsAssignableFrom(e.ClrType))
            .Select(e => e.ClrType);

        foreach (var entity in entities)
        {
            var entityBuilder = modelBuilder.Entity(entity);
            var clrType = entityBuilder.Metadata.ClrType;
            var parameterType = Expression.Parameter(clrType);

            var filterBody = ReplacingExpressionVisitor.Replace(
                filter.Parameters.Single(),
                parameterType,
                filter.Body);

            // EF Core 10: returns IReadOnlyList<IQueryFilter>
            var existingFilters = entityBuilder.Metadata.GetDeclaredQueryFilters();

            foreach (var existingFilter in existingFilters)
            {
                var existingExpression = existingFilter.Expression;
                if (existingExpression is not null)
                {
                    var existingFilterBody = ReplacingExpressionVisitor.Replace(
                        existingExpression.Parameters.Single(),
                        parameterType,
                        existingExpression.Body);
                    filterBody = Expression.AndAlso(existingFilterBody, filterBody);
                }
            }

            entityBuilder.HasQueryFilter(Expression.Lambda(filterBody, parameterType));
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