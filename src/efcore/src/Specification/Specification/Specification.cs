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
        /// Add or update expression
        /// </summary>
        /// <param name="expression"></param>
        protected void Where(Expression<Func<T, bool>> expression)
        {
            if (Expression is null)
            {
                Expression = expression;
            }
            else
            {
                InvocationExpression invocationExpression = System.Linq.Expressions.Expression.Invoke(Expression, expression.Parameters.Cast<Expression>());
                Expression = System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(System.Linq.Expressions.Expression.AndAlso(expression.Body, invocationExpression), (IEnumerable<ParameterExpression>)expression.Parameters);
            }
        }

        /// <summary>
        /// Add or update expression when condition is true
        /// </summary>
        /// <param name="expression"></param>
        protected void WhereIf(bool condition, Expression<Func<T, bool>> expression)
        {
            if (condition)
            {
                Where(expression);
            }
        }
    }
}
