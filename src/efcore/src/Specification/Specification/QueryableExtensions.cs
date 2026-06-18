using System;
using System.Linq;
using System.Linq.Expressions;

namespace Light.Specification
{
    public static class QueryableExtensions
    {
        public static IQueryable<T> Where<T>(this IQueryable<T> source, ISpecification<T> specification)
            where T : class
        {
            if (specification?.Expression != null) source = source.Where(specification.Expression);
            return source;
        }

        public static IQueryable<T> WhereIf<T>(this IQueryable<T> source, bool condition, Expression<Func<T, bool>> expression)
        {
            if (condition) source = source.Where(expression);
            return source;
        }

        public static IQueryable<T> WhereIf<T>(this IQueryable<T> source, bool condition, ISpecification<T> specification)
            where T : class
        {
            if (specification?.Expression != null && condition) source = source.Where(specification.Expression);
            return source;
        }

        public static IQueryable<T> Apply<T>(this IQueryable<T> source, ISpecification<T> specification) where T : class
        {
            if (specification?.Expression != null) source = source.Where(specification.Expression);
            if (specification is Specification<T> spec)
            {
                IOrderedQueryable<T>? ordered = null;
                foreach (var ob in spec.OrderByExpressions)
                {
                    ordered = ordered == null
                        ? (ob.IsDescending ? source.OrderByDescending(ob.KeySelector) : source.OrderBy(ob.KeySelector))
                        : (ob.IsDescending ? ordered.ThenByDescending(ob.KeySelector) : ordered.ThenBy(ob.KeySelector));
                }
                if (ordered != null) source = ordered;
                if (spec.Skip.HasValue) source = source.Skip(spec.Skip.Value);
                if (spec.Take.HasValue) source = source.Take(spec.Take.Value);
            }
            return source;
        }
    }
}