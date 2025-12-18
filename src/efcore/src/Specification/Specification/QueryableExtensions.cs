using Light.Specification;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Light.Specification
{
    public static class QueryableExtensions
    {
        /// <summary>
        ///     Filter data by specification
        /// </summary>
        public static IQueryable<T> Where<T>(this IQueryable<T> query, ISpecification<T> specification)
        {
            if (specification?.Expression != null)
            {
                return query.Where(specification.Expression);
            }

            return query;
        }

        /// <summary>
        ///     Only filter data when condition is true
        /// </summary>
        public static IQueryable<T> WhereIf<T>(this IQueryable<T> query, bool condition, Expression<Func<T, bool>> expression)
        {
            if (condition)
            {
                return query.Where(expression);
            }

            return query;
        }

        /// <summary>
        ///     Only filter data by specification when condition is true
        /// </summary>
        public static IQueryable<T> WhereIf<T>(this IQueryable<T> query, bool condition, ISpecification<T> specification)
        {
            if (specification?.Expression != null && condition)
            {
                return query.Where(specification.Expression);
            }

            return query;
        }
    }
}