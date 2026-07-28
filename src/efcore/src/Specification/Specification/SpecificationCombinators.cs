using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Light.Specification
{
    public static class SpecificationCombinators
    {
        public static ISpecification<T> And<T>(this ISpecification<T> left, ISpecification<T> right) where T : class
        {
            if (left.Expression == null) return right;
            if (right.Expression == null) return left;
            var p = left.Expression.Parameters[0];
            var rb = new ParameterReplacer(right.Expression.Parameters[0], p).Visit(right.Expression.Body);
            var expression = Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left.Expression.Body, rb), p);
            return CombineOrdering(expression, left, right);
        }

        public static ISpecification<T> Or<T>(this ISpecification<T> left, ISpecification<T> right) where T : class
        {
            if (left.Expression == null || right.Expression == null) return CombineOrdering<T>(null, left, right);
            var p = left.Expression.Parameters[0];
            var rb = new ParameterReplacer(right.Expression.Parameters[0], p).Visit(right.Expression.Body);
            var expression = Expression.Lambda<Func<T, bool>>(Expression.OrElse(left.Expression.Body, rb), p);
            return CombineOrdering(expression, left, right);
        }

        public static ISpecification<T> Not<T>(this ISpecification<T> spec) where T : class
        {
            if (spec.Expression == null) return CombineOrdering<T>(x => false, spec, null);
            var p = spec.Expression.Parameters[0];
            var expression = Expression.Lambda<Func<T, bool>>(Expression.Not(spec.Expression.Body), p);
            return CombineOrdering(expression, spec, null);
        }

        /// <summary>
        /// Builds the combinator result, preserving ordering/paging from <paramref name="primary"/>
        /// (falling back to <paramref name="secondary"/>) when either operand is an
        /// <see cref="IOrderedSpecification{T}"/> — e.g. a <see cref="Specification{T}"/> — with
        /// ordering or paging actually set. Without this, combinator output silently dropped
        /// ordering/paging even when an operand carried it.
        /// </summary>
        private static InlineSpecification<T> CombineOrdering<T>(
            Expression<Func<T, bool>>? expression,
            ISpecification<T>? primary,
            ISpecification<T>? secondary)
            where T : class
        {
            if (primary is IOrderedSpecification<T> p && HasOrdering(p))
                return new InlineSpecification<T>(expression, p.OrderByExpressions, p.Skip, p.Take);
            if (secondary is IOrderedSpecification<T> s && HasOrdering(s))
                return new InlineSpecification<T>(expression, s.OrderByExpressions, s.Skip, s.Take);
            return new InlineSpecification<T>(expression);
        }

        private static bool HasOrdering<T>(IOrderedSpecification<T> spec) where T : class
            => spec.OrderByExpressions.Count > 0 || spec.Skip.HasValue || spec.Take.HasValue;

        private sealed class InlineSpecification<T> : IOrderedSpecification<T>
            where T : class
        {
            public Expression<Func<T, bool>>? Expression { get; }
            public IReadOnlyList<OrderByExpression<T>> OrderByExpressions { get; }
            public int? Skip { get; }
            public int? Take { get; }

            public InlineSpecification(
                Expression<Func<T, bool>>? expression,
                IReadOnlyList<OrderByExpression<T>>? orderByExpressions = null,
                int? skip = null,
                int? take = null)
            {
                Expression = expression;
                OrderByExpressions = orderByExpressions ?? Array.Empty<OrderByExpression<T>>();
                Skip = skip;
                Take = take;
            }
        }

        private sealed class ParameterReplacer : ExpressionVisitor
        {
            private readonly ParameterExpression _oldParam;
            private readonly ParameterExpression _newParam;
            public ParameterReplacer(ParameterExpression o, ParameterExpression n) { _oldParam = o; _newParam = n; }
            protected override Expression VisitParameter(ParameterExpression node) => node == _oldParam ? _newParam : base.VisitParameter(node);
        }
    }
}
