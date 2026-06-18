using System;
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
            return new InlineSpecification<T>(Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left.Expression.Body, rb), p));
        }

        public static ISpecification<T> Or<T>(this ISpecification<T> left, ISpecification<T> right) where T : class
        {
            if (left.Expression == null || right.Expression == null) return new InlineSpecification<T>(null);
            var p = left.Expression.Parameters[0];
            var rb = new ParameterReplacer(right.Expression.Parameters[0], p).Visit(right.Expression.Body);
            return new InlineSpecification<T>(Expression.Lambda<Func<T, bool>>(Expression.OrElse(left.Expression.Body, rb), p));
        }

        public static ISpecification<T> Not<T>(this ISpecification<T> spec) where T : class
        {
            if (spec.Expression == null) return new InlineSpecification<T>(x => false);
            var p = spec.Expression.Parameters[0];
            return new InlineSpecification<T>(Expression.Lambda<Func<T, bool>>(Expression.Not(spec.Expression.Body), p));
        }

        private sealed class InlineSpecification<T> : ISpecification<T>
        {
            public Expression<Func<T, bool>>? Expression { get; }
            public InlineSpecification(Expression<Func<T, bool>>? expression) { Expression = expression; }
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