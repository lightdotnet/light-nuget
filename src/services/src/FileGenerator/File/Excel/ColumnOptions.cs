using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Light.File.Excel
{
    public class ColumnOptions<T>
    {
        public ColumnOptions(Action<ColumnOptions<T>> action)
        {
            action.Invoke(this);
        }

        public Dictionary<string, string> ColumnNames { get; private set; } = new Dictionary<string, string>();

        public void SetColumn<TResult>(Expression<Func<T, TResult>> expr, string columnHeader)
        {
            var propName = GetPropertyName(expr);

            SetCustomHeader(propName, columnHeader);
        }

        internal void SetCustomHeader(string propName, string customHeader)
        {
            if (ColumnNames.ContainsKey(propName))
                ColumnNames[propName] = customHeader;
            else
                ColumnNames.Add(propName, customHeader);
        }

        internal string GetPropertyName<TResult>(Expression<Func<T, TResult>> expr)
        {
            if (!(expr.Body is MemberExpression memberExpression))
                throw new ArgumentException($"The provided expression contains a {expr.GetType().Name} which is not supported. Only simple member accessors (fields, properties) of an object are supported.");
            return memberExpression.Member.Name;
        }
    }
}