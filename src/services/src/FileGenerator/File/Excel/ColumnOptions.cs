using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Light.File.Excel
{
    public class ColumnOptions<T>
    {
        public Dictionary<string, string> ColumnNames { get; } = new Dictionary<string, string>();

        public ColumnOptions<T> SetColumn<TResult>(Expression<Func<T, TResult>> expr, string columnHeader)
        {
            ColumnNames[GetPropertyName(expr)] = columnHeader;
            return this; // fluent
        }

        private static string GetPropertyName<TResult>(Expression<Func<T, TResult>> expr)
        {
            if (!(expr.Body is MemberExpression memberExpression))
                throw new ArgumentException($"The provided expression contains a {expr.GetType().Name} which is not supported. Only simple member accessors (fields, properties) of an object are supported.");
            return memberExpression.Member.Name;
        }
    }
}