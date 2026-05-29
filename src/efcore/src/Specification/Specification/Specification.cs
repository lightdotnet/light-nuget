using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Light.Specification
{
    public abstract class Specification<T> : ISpecification<T>
        where T : class
    {
        public Expression<Func<T, bool>>? Expression { get; private set; }

        /// <summary>
        /// Lazy compiled delegate for in-memory filtering
        /// </summary>
        private Func<T, bool>? _compiledExpression;
        public Func<T, bool>? CompiledExpression
            => Expression != null
                ? (_compiledExpression ??= Expression.Compile())
                : null;


        /// <summary>
        /// Add or update expression
        /// </summary>
        protected void Where(Expression<Func<T, bool>> expression)
        {
            if (Expression is null)
            {
                Expression = expression;
            }
            else
            {
                var parameter = expression.Parameters[0];
                var left = new ParameterReplacer(Expression.Parameters[0], parameter)
                    .Visit(Expression.Body);
                var combined = System.Linq.Expressions.Expression.AndAlso(left, expression.Body);
                Expression = System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(combined, parameter);

                _compiledExpression = null;
            }
        }

        /// <summary>
        /// Add or update expression when condition is true
        /// </summary>
        protected void WhereIf(bool condition, Expression<Func<T, bool>> expression)
        {
            if (condition)
            {
                Where(expression);
            }
        }

        /// <summary>
        /// Replace parameter references in an expression tree
        /// </summary>
        private sealed class ParameterReplacer : ExpressionVisitor
        {
            private readonly ParameterExpression _oldParam;
            private readonly ParameterExpression _newParam;

            public ParameterReplacer(ParameterExpression oldParam, ParameterExpression newParam)
            {
                _oldParam = oldParam;
                _newParam = newParam;
            }

            protected override Expression VisitParameter(ParameterExpression node)
                => node == _oldParam ? _newParam : base.VisitParameter(node);
        }

    }
}