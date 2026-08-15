using System;
using System.Linq.Expressions;

namespace Light.Specification
{
    public class OrderByExpression<T> where T : class
    {
        public Expression<Func<T, object>> KeySelector { get; }
        public bool IsDescending { get; }

        public OrderByExpression(Expression<Func<T, object>> keySelector, bool isDescending)
        {
            KeySelector = keySelector;
            IsDescending = isDescending;
        }
    }
}