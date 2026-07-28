using System.Collections.Generic;

namespace Light.Specification
{
    /// <summary>
    /// A specification that additionally carries ordering and paging information.
    /// Implemented by <see cref="Specification{T}"/>; combinator results produced by
    /// <see cref="SpecificationCombinators"/> implement it too, preserving ordering/paging
    /// pulled from their operand(s) where present.
    /// Kept as a separate, class-constrained interface (rather than adding these members to
    /// <see cref="ISpecification{T}"/> itself) so <c>ISpecification&lt;T&gt;</c> remains usable
    /// for any <c>T</c>, including value types, without forcing them to carry ordering state.
    /// </summary>
    public interface IOrderedSpecification<T> : ISpecification<T>
        where T : class
    {
        IReadOnlyList<OrderByExpression<T>> OrderByExpressions { get; }
        int? Skip { get; }
        int? Take { get; }
    }
}
