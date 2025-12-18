using Light.Specification;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Light.Specification
{
    public static class CollectionExtensions
    {
        /// <summary>
        ///     Filter a collection by specification
        /// </summary>
        public static IEnumerable<T> Where<T>(this IEnumerable<T> source, ISpecification<T> specification)
        {
            if (specification?.Expression != null)
            {
                Func<T, bool> func = specification.Expression.Compile();
                return source.Where(func);
            }

            return source;
        }

        /// <summary>
        ///     Only filter a collection by specification when condition is true
        /// </summary>
        public static IEnumerable<T> WhereIf<T>(this IEnumerable<T> source, bool condition, ISpecification<T> specification)
        {
            if (specification?.Expression != null && condition)
            {
                Func<T, bool> func = specification.Expression.Compile();
                return source.Where(func);
            }

            return source;
        }
    }
}