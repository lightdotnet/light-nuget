using System.Collections.Generic;
using System.Linq;

namespace Light.Specification
{
    public static class CollectionExtensions
    {
        public static IEnumerable<T> Where<T>(this IEnumerable<T> source, ISpecification<T> specification)
            where T : class
        {
            if (specification?.Expression == null) return source;
            var compiled = (specification as Specification<T>)?.CompiledExpression ?? specification.Expression.Compile();
            return source.Where(compiled);
        }

        public static IEnumerable<T> WhereIf<T>(this IEnumerable<T> source, bool condition, ISpecification<T> specification)
            where T : class
        {
            if (!condition || specification?.Expression == null) return source;
            var compiled = (specification as Specification<T>)?.CompiledExpression ?? specification.Expression.Compile();
            return source.Where(compiled);
        }
    }
}