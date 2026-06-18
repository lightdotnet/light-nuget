using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Light.Specification
{
    public abstract class Specification<T> : ISpecification<T>
        where T : class
    {
        public Expression<Func<T, bool>>? Expression { get; private set; }

        private Func<T, bool>? _compiledExpression;
        public Func<T, bool>? CompiledExpression
            => Expression != null
                ? (_compiledExpression ??= Expression.Compile())
                : null;

        public IReadOnlyList<OrderByExpression<T>> OrderByExpressions => _orderByExpressions;
        private readonly List<OrderByExpression<T>> _orderByExpressions = new List<OrderByExpression<T>>();

        public int? Skip { get; private set; }
        public int? Take { get; private set; }

        public bool IsSatisfiedBy(T entity)
        {
            if (CompiledExpression == null) return true;
            return CompiledExpression(entity);
        }

        protected void Where(Expression<Func<T, bool>> expression)
        {
            if (Expression == null)
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

        protected void WhereIf(bool condition, Expression<Func<T, bool>> expression)
        {
            if (condition) Where(expression);
        }

        protected void OrderBy(Expression<Func<T, object>> keySelector)
        {
            _orderByExpressions.Add(new OrderByExpression<T>(keySelector, false));
        }

        protected void OrderByDescending(Expression<Func<T, object>> keySelector)
        {
            _orderByExpressions.Add(new OrderByExpression<T>(keySelector, true));
        }

        protected void ApplyPaging(int skip, int take)
        {
            Skip = skip;
            Take = take;
        }

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